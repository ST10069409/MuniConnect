namespace MuniConnect.Core.Dsa;

/// <summary>Binary tree. Each node has a left child and a right child.</summary>
public sealed class BinaryTree<T>
{
    public BinaryTree(T value, BinaryTree<T>? left = null, BinaryTree<T>? right = null)
    {
        Value = value;
        Left = left;
        Right = right;
    }

    public T Value { get; }
    public BinaryTree<T>? Left { get; }
    public BinaryTree<T>? Right { get; }

    public int Size => 1 + (Left?.Size ?? 0) + (Right?.Size ?? 0);

    public int Height => 1 + Math.Max(Left?.Height ?? 0, Right?.Height ?? 0);

    public List<T> Preorder() => Collect(WalkOrder.Pre);
    public List<T> Inorder() => Collect(WalkOrder.In);
    public List<T> Postorder() => Collect(WalkOrder.Post);

    private List<T> Collect(WalkOrder order)
    {
        var values = new List<T>();
        void Walk(BinaryTree<T>? node)
        {
            if (node == null)
            {
                return;
            }

            if (order == WalkOrder.Pre)
            {
                values.Add(node.Value);
            }

            Walk(node.Left);
            if (order == WalkOrder.In)
            {
                values.Add(node.Value);
            }

            Walk(node.Right);
            if (order == WalkOrder.Post)
            {
                values.Add(node.Value);
            }
        }

        Walk(this);
        return values;
    }

    private enum WalkOrder { Pre, In, Post }
}
