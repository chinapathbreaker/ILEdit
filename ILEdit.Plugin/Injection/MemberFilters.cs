using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ILEdit.Injection
{
    /// <summary>
    /// Static class containing the most common member filters
    /// </summary>
    public static class MemberFilters
    {
        #region All

        /// <summary>
        /// Filter used to show everything
        /// </summary>
        public static Predicate<IMetadataTokenProvider> All 
        {
            get { return x => true; }
        }

        #endregion

        #region Assemblies

        /// <summary>
        /// Filter used to show only the Assemblies
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Assemblies
        {
            get { return x => x.MetadataToken.TokenType == TokenType.Assembly; }
        }

        #endregion

        #region Modules
        
        /// <summary>
        /// Filter used to show up to the modules
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Modules
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                            return true;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Types

        /// <summary>
        /// Filter used to show up to the types (classes, structs, interfaces, ...)
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Types
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                        case TokenType.TypeDef:
                            return true;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Classes

        /// <summary>
        /// Filter used to show only classes
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Classes
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                            return true;
                        case TokenType.TypeDef:
                            var type = (TypeDefinition)x;
                            return !type.IsValueType && !type.IsInterface && !(type.BaseType != null && type.BaseType.FullName == typeof(MulticastDelegate).FullName);
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Structures

        /// <summary>
        /// Filter used to show only structures
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Structures
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                            return true;
                        case TokenType.TypeDef:
                            return ((TypeDefinition)x).IsValueType && !((TypeDefinition)x).IsEnum;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// Filter used to show only enums
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Enums
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                            return true;
                        case TokenType.TypeDef:
                            return ((TypeDefinition)x).IsEnum;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Delegates

        /// <summary>
        /// Filter used to show only delegates
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Delegates
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                            return true;
                        case TokenType.TypeDef:
                            var type = (TypeDefinition)x;
                            return type.BaseType != null && type.BaseType.FullName == typeof(MulticastDelegate).FullName;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Interfaces

        /// <summary>
        /// Filter used to show only interfaces
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Interfaces
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                            return true;
                        case TokenType.TypeDef:
                            return ((TypeDefinition)x).IsInterface;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Filter used to show only fields
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Fields
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                        case TokenType.TypeDef:
                        case TokenType.Field:
                            return true;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Filter used to show only methods
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Methods
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                        case TokenType.TypeDef:
                        case TokenType.Method:
                            return true;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Filter used to show only constructors
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Constructors
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                        case TokenType.TypeDef:
                            return true;
                        case TokenType.Method:
                            var method = (MethodDefinition)x;
                            return method.IsSpecialName && (method.Name == ".ctor" || method.Name == ".cctor");
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Filter used to show only properties
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Properties
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                        case TokenType.TypeDef:
                        case TokenType.Property:
                            return true;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Filter used to show only events
        /// </summary>
        public static Predicate<IMetadataTokenProvider> Events
        {
            get
            {
                return x =>
                {
                    switch (x.MetadataToken.TokenType)
                    {
                        case TokenType.Module:
                        case TokenType.Assembly:
                        case TokenType.TypeDef:
                        case TokenType.Event:
                            return true;
                        default:
                            return false;
                    }
                };
            }
        }

        #endregion
    }
}
