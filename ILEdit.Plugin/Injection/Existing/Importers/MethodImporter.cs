using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILEdit.Injection.Existing.Importers
{
    /// <summary>
    /// Represents a method importer
    /// </summary>
    public class MethodImporter : MemberImporter
    {
        private MethodDefinition methodClone;
        private bool _createNode;

        public MethodImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session)
            : this(member, destination, session, true)
        {
        }

        public MethodImporter(IMetadataTokenProvider member, IMetadataTokenProvider destination, MemberImportingSession session, bool createNode)
            : base(member, destination, session)
        {
            _createNode = createNode;
        }

        protected override bool CanImportCore(Mono.Cecil.IMetadataTokenProvider member, Mono.Cecil.IMetadataTokenProvider destination)
        {
            return member.MetadataToken.TokenType == TokenType.Method && destination.MetadataToken.TokenType == TokenType.TypeDef;
        }

        protected override void ScanCore(MemberImportingOptions options, List<MemberImporter> importList)
        {
            //Method
            var originalMethod = (MethodDefinition)Member;
            methodClone = originalMethod.Clone(Session);

            //Imports the generic parameters
            if (methodClone.HasGenericParameters)
            {
                importList.Add(new GenericParametersImporter(methodClone, methodClone, Session).Scan(options));
                methodClone.GenericParameters.Clear();
            }

            //Imports the attributes
            if (methodClone.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(methodClone, methodClone, Session).Scan(options));
                methodClone.CustomAttributes.Clear();
            }

            //Imports the return type
            var retImporter = Helpers.CreateTypeImporter(originalMethod.ReturnType, Session, importList, options);
            retImporter.ImportFinished += t => methodClone.ReturnType = (TypeReference)t;
            
            //Imports the attributes of the return type
            if (methodClone.MethodReturnType.HasCustomAttributes)
            {
                importList.Add(new CustomAttributesImporter(methodClone.MethodReturnType, methodClone.MethodReturnType, Session).Scan(options));
                methodClone.MethodReturnType.CustomAttributes.Clear();
            }

            //Imports the parameters
            foreach (var p in originalMethod.Parameters)
            {
                //Creates a new parameter
                var param = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType)
                {
                    Constant = p.Constant,
                    MarshalInfo = p.MarshalInfo,
                    MetadataToken = new MetadataToken(p.MetadataToken.TokenType, GlobalContainer.GetFreeRID(Session.DestinationModule))
                };
                methodClone.Parameters.Add(param);

                //Queues importing of custom attributes
                if (p.HasCustomAttributes)
                {
                    importList.Add(new CustomAttributesImporter(p, param, Session).Scan(options));
                    param.CustomAttributes.Clear();
                }

                //Queues importing of type
                var typeImporter = Helpers.CreateTypeImporter(p.ParameterType, Session, importList, options);
                typeImporter.ImportFinished += t => param.ParameterType = (TypeReference)t;
            }

            //Clones the body
            var originalBody = originalMethod.Body;
            var body = methodClone.Body;
            body.InitLocals = originalBody.InitLocals;
            body.MaxStackSize = originalBody.MaxStackSize;
            
            //Clones the variables
            foreach (var v in originalBody.Variables)
            {
                var var = new VariableDefinition(v.Name, v.VariableType);
                var typeImporter = Helpers.CreateTypeImporter(var.VariableType, Session, importList, options);
                typeImporter.ImportFinished += t => var.VariableType = (TypeReference)t;
                body.Variables.Add(var);
            }

            //Clones the instructions
            foreach (var x in originalBody.Instructions)
            {
                //Creates a new instruction with the same opcode
                var i = x;
                var instruction = Instruction.Create(OpCodes.Nop);
                instruction.OpCode = i.OpCode;
                body.Instructions.Add(instruction);
                instruction.Offset = i.Offset;

                //Switches on the type of the opcode to 
                switch (i.OpCode.OperandType)
                {
                    //Delays the importing of the operand
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        var index = originalBody.Instructions.IndexOf((Instruction)i.Operand);
                        importList.Add(MemberImporter.Create((_, __) => {
                            instruction.Operand = body.Instructions[index];
                            return null;
                        }));
                        break;
                    case OperandType.InlineSwitch:
                        var indexes = ((Instruction[])i.Operand).Select(a => originalBody.Instructions.IndexOf(a)).ToArray();
                        importList.Add(MemberImporter.Create((_, __) => {
                            instruction.Operand = indexes.Select(a => body.Instructions[a]).ToArray();
                            return null;
                        }));
                        break;

                    //Registers importing of the type
                    case OperandType.InlineType:
                        var importer = Helpers.CreateTypeImporter((TypeReference)i.Operand, Session, importList, options);
                        importer.ImportFinished += t => instruction.Operand = t;
                        break;

                    //Registers importing of the declaring type
                    //along with the field type
                    case OperandType.InlineField:
                        var field = (FieldReference)i.Operand;
                        field = new FieldReference(field.Name, field.FieldType, field.DeclaringType);
                        Helpers.CreateTypeImporter(field.DeclaringType, Session, importList, options)
                            .ImportFinished += t => field.DeclaringType = (TypeReference)t;
                        Helpers.CreateTypeImporter(field.FieldType, Session, importList, options)
                            .ImportFinished += t => field.FieldType = (TypeReference)t;
                        instruction.Operand = field;
                        break;
                    
                    //Registers importing of the method
                    case OperandType.InlineMethod:
                        var m = (MethodReference)i.Operand;
                        var methodImporter = MemberImporter.Create((_, __) => {
                            instruction.Operand = Session.DestinationModule.Import(m);
                            return null;
                        });
                        importList.Add(methodImporter);
                        break;

                    //Keeps the same operand
                    default:
                        instruction.Operand = i.Operand;
                        break;
                }
            }

            //Imports the overrides
            if (methodClone.HasOverrides)
            {
                foreach (var x in methodClone.Overrides)
                {
                    var o = x;
                    importList.Add(MemberImporter.Create((_, __) => {
                        methodClone.Overrides.Add(Session.DestinationModule.Import(o));
                        return null;
                    }));
                }
                methodClone.Overrides.Clear();
            }

        }

        protected override IEnumerable<IMetadataTokenProvider> GetMembersForPreview()
        {
            return _createNode ? base.GetMembersForPreview() : base.GetMembersForPreview().Except(new[] { Member });
        }

        protected override Mono.Cecil.IMetadataTokenProvider ImportCore(MemberImportingOptions options, ICSharpCode.TreeView.SharpTreeNode node)
        {
            //Checks that the task hasn't been canceled
            options.CancellationToken.ThrowIfCancellationRequested();

            //Adds the field to the destination type
            ((TypeDefinition)Destination).Methods.Add(methodClone);
            if (_createNode)
                node.AddChildAndColorAncestors(new ILEditTreeNode(methodClone, false));

            //Returns the new field
            return methodClone;
        }

        protected override void DisposeCore()
        {
            methodClone = null;
        }
    }
}
