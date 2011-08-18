using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;

namespace ILEdit.Injection
{
    /// <summary>
    /// Interface for all the injectors
    /// </summary>
    public interface IInjector
    {
        /// <summary>
        /// Returns the name of this injector displayed to the user
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the description displayed to the user
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Representative icon of the injector
        /// </summary>
        ImageSource Icon { get; }

        /// <summary>
        /// Returns a value indicating whether this injector needs a member specification or not
        /// </summary>
        bool NeedsMember { get; }

        /// <summary>
        /// Returns a predicate used to filter the valid members (if NeedsType is true)
        /// </summary>
        Predicate<IMetadataTokenProvider> MemberFilter { get; }

        /// <summary>
        /// Returns a TokenType representing the members that the user can choose (if NeedsType is true)
        /// </summary>
        TokenType SelectableMembers { get; }

        /// <summary>
        /// Determines if this injector can inject in the given node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        bool CanInjectInNode(ILSpyTreeNode node);

        /// <summary>
        /// Injects in the given node
        /// </summary>
        /// <param name="node">Destination node</param>
        /// <param name="name">Name of the new object to inject</param>
        /// <param name="member">Member selected by the user (null if NeedsMember is false)</param>
        void Inject(ILSpyTreeNode node, string name, IMetadataTokenProvider member);
    }
}
