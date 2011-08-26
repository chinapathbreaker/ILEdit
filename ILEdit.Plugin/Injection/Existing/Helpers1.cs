 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection.Existing
{
    internal static partial class Helpers
    {
        #region FieldDefinition.Clone() extension
        
        /// <summary>
        /// Clones this field
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static FieldDefinition Clone(this FieldDefinition field)
        {
            var f = new FieldDefinition(field.Name, field.Attributes, field.FieldType) 
                {
					Offset = field.Offset,
					InitialValue = field.InitialValue,
					HasConstant = field.HasConstant,
					Constant = field.Constant,
					MarshalInfo = field.MarshalInfo,
					HasDefault = field.HasDefault,
					MetadataToken = field.MetadataToken
                };
			foreach (var x in field.CustomAttributes)
				f.CustomAttributes.Add(x);
			return f;
        }

        #endregion
		
		#region CustomAttribute.Clone() extension
        
        /// <summary>
        /// Clones this custom attribute
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static CustomAttribute Clone(this CustomAttribute attr)
        {
            var a = new CustomAttribute(attr.Constructor) 
                {

                };
			foreach (var x in attr.ConstructorArguments)
				a.ConstructorArguments.Add(x);
			foreach (var x in attr.Fields)
				a.Fields.Add(x);
			foreach (var x in attr.Properties)
				a.Properties.Add(x);
			return a;
        }

        #endregion
		
		#region TypeDefinition.Clone() extension
        
        /// <summary>
        /// Clones this type
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public static TypeDefinition Clone(this TypeDefinition type)
        {
            var t = new TypeDefinition(type.Namespace, type.Name, type.Attributes, type.BaseType) 
                {
					PackingSize = type.PackingSize,
					ClassSize = type.ClassSize,
					HasSecurity = type.HasSecurity,
					MetadataToken = type.MetadataToken
                };
			foreach (var x in type.Interfaces)
				t.Interfaces.Add(x);
			foreach (var x in type.NestedTypes)
				t.NestedTypes.Add(x);
			foreach (var x in type.Methods)
				t.Methods.Add(x);
			foreach (var x in type.Fields)
				t.Fields.Add(x);
			foreach (var x in type.Events)
				t.Events.Add(x);
			foreach (var x in type.Properties)
				t.Properties.Add(x);
			foreach (var x in type.SecurityDeclarations)
				t.SecurityDeclarations.Add(x);
			foreach (var x in type.CustomAttributes)
				t.CustomAttributes.Add(x);
			foreach (var x in type.GenericParameters)
				t.GenericParameters.Add(x);
			return t;
        }

        #endregion
    }
}