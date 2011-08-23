﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ILEdit.Injection;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using System.Windows;

namespace ILEdit.Injection.Injectors
{
    /// <summary>
    /// Enum injector
    /// </summary>
    public class EnumInjector : IInjector
    {
        #region Properties

        public string Name
        {
            get { return "Enum"; }
        }

        public string Description
        {
            get { return "Injects a new enum and the relative field value__"; }
        }

        public System.Windows.Media.ImageSource Icon
        {
            get { return new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/ILSpy;component/Images/Enum.png")); }
        }

        public bool NeedsMember
        {
            get { return false; }
        }

        public Predicate<Mono.Cecil.IMetadataTokenProvider> MemberFilter
        {
            get { return null; }
        }

        public TokenType[] SelectableMembers
        {
            get { return new TokenType[] { }; }
        }

        #endregion

        public bool CanInjectInNode(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node)
        {
            //Try-cast
            var memberNode = node as IMemberTreeNode;
            var type = memberNode == null ? null : (memberNode.Member as TypeDefinition);

            //Can inject only in modules and other existing types (except enums and interfaces)
            return
                node is ModuleTreeNode ||
                (memberNode != null &&
                    type != null &&
                    !type.IsEnum &&
                    !type.IsInterface
                );
        }

        public void Inject(ICSharpCode.ILSpy.TreeNodes.ILSpyTreeNode node, string name, IMetadataTokenProvider member)
        {
            //Name and namespace
            var typeName = node is ModuleTreeNode ? (name.Substring(name.Contains(".") ? name.LastIndexOf('.') + 1 : 0)) : name;
            var typeNamespace = node is ModuleTreeNode ? (name.Substring(0, name.Contains(".") ? name.LastIndexOf('.') : 0)) : string.Empty;

            //Checks that the typename isn't empty
            if (string.IsNullOrEmpty(typeName))
            {
                MessageBox.Show("Please, specify the name of the type", "Type name required", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //Creates a new class definition
            var c = new TypeDefinition(
                typeNamespace,
                typeName,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed
            )
            {
                IsAnsiClass = true,
            };

            //Destination module
            ModuleDefinition module = null;

            //Adds to the node and specifies the base class
            TreeHelper.AddTreeNode(node, c,
                m => { module = m; c.BaseType = new TypeReference("System", "Enum", m, m.TypeSystem.Corlib, true); },
                t => { module = t.Module; c.BaseType = new TypeReference("System", "Enum", t.Module, t.Module.TypeSystem.Corlib, true); }
            );

            //Injects the field value___
            c.Fields.Add(new FieldDefinition(
                "value__",
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName,
                module.TypeSystem.Int32
            ));
        }
    }
}
