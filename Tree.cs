using System;
using System.Collections.Generic;
using ApiCommon.Extensions;

namespace ApiCommon
{
    public interface IId<out T>
    {
        T Id { get; }
    }

    public class Tree<TId, T> : IId<TId> where T : Tree<TId, T>
    {
        private readonly IDictionary<TId, T> _children = new Dictionary<TId, T>();

        public Tree(TId id)
        {
            if (id.Equals(default(TId)))
                throw new ArgumentException("Cannot create tree node with an empty ID");

            Id = id;
        } 

        public TId Id { get; }
        public T Parent { get; set; }
        public T Root => Parent == null ? (T)this : Parent.Root;
        public ICollection<T> Children => _children?.Values;

        public Tree<TId, T> GetChild(TId id)
        {
            return _children.GetOrDefault(id);
        }

        public T AddChild(T child)
        {
            if (child != null && HasNoLoop(child))
            {
                _children[child.Id] = child;
                child.Parent = (T)this;
                return child; // good for chaining e.g. AddChild(x).AddChild(y)
            }
            return null;
        }

        public bool RemoveChild(TId id)
        {
            if (_children.ContainsKey(id))
            {
                _children.Remove(id);
                return true;
            }
            return false;
        }

        public T SearchDown(TId id)
        {
            if (IdEquals(id))
                return (T)this;

            foreach (var child in _children)
            {
                var found = child.Value.SearchDown(id);
                if (found != null)
                    return found;
            }
            return null;
        }

        public T Find(TId id)
        {
            return Root.SearchDown(id);
        }

        protected virtual bool IdEquals(TId other)
        {
            return Id.Equals(other);
        }

        public ICollection<T> All => Root.GetDescendants();

        public ICollection<T> GetDescendants()
        {
            var list = new List<T>();
            GetDescendants(list);
            return list;
        }

        private void GetDescendants(ICollection<T> col)
        {
            col.Add((T)this);
            foreach (var child in _children.Values)
            {
                child.GetDescendants(col);
            }
        }

        /// <summary>Transforms the tree to a tree of another type with same hierarchical structure.</summary>
        public Tree<TId2, T2> Transform<TId2, T2>(Func<Tree<TId, T>, Tree<TId2, T2>> nodeConverter) where T2: Tree<TId2, T2>
        {
            return Root.TransformImpl(nodeConverter);
        }

        private Tree<TId2, T2> TransformImpl<TId2, T2>(Func<Tree<TId, T>, Tree<TId2, T2>> nodeConverter) where T2 : Tree<TId2, T2>
        {
            var transformedNode = nodeConverter(this);
            foreach (var child in _children.Values)
            {
                var transformedChild = child.TransformImpl(nodeConverter);
                transformedNode.AddChild((T2)transformedChild);
            }
            return transformedNode;
        }

        private bool HasNoLoop(T newChild)
        {
            if (_children.ContainsKey(newChild.Id))
                return true; // just replace the child, it's OK

            return Find(newChild.Id) == null;
        }
    }

    public class TreeOfCaseInsensitiveStringID : Tree<string, TreeOfCaseInsensitiveStringID>
    {
        public TreeOfCaseInsensitiveStringID(string id) : base(id) {  }

        protected override bool IdEquals(string id)
        {
            return Id.Equals(id, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
