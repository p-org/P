using System;
using System.Collections.Generic;

namespace PChecker.SystematicTesting.Strategies.Feedback;

public class MaxHeap<TValue>
{
    public readonly List<TValue> Elements = new();
    private IComparer<TValue> _comparer;

    public MaxHeap(IComparer<TValue> cmp)
    {
        _comparer = cmp;
    }

    private int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
    private int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
    private int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

    private bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < Elements.Count;
    private bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < Elements.Count;
    private bool IsRoot(int elementIndex) => elementIndex == 0;

    private TValue GetLeftChild(int elementIndex) => Elements[GetLeftChildIndex(elementIndex)];
    private TValue GetRightChild(int elementIndex) => Elements[GetRightChildIndex(elementIndex)];
    private TValue GetParent(int elementIndex) => Elements[GetParentIndex(elementIndex)];

    private void Swap(int firstIndex, int secondIndex)
    {
        (Elements[firstIndex], Elements[secondIndex]) = (Elements[secondIndex], Elements[firstIndex]);
    }

    public TValue Peek()
    {
        return Elements[0];
    }

    public TValue Pop()
    {
        var result = Elements[0];
        Elements[0] = Elements[Elements.Count - 1];
        Elements.RemoveAt(Elements.Count - 1);

        ReCalculateDown();

        return result;
    }

    public void Add(TValue element)
    {
        Elements.Add(element);
        ReCalculateUp();
    }

    private void ReCalculateDown()
    {
        int index = 0;
        while (HasLeftChild(index))
        {
            var biggerIndex = GetLeftChildIndex(index);
            if (HasRightChild(index) &&
                _comparer.Compare(GetRightChild(index), GetLeftChild(index)) > 0)
            {
                biggerIndex = GetRightChildIndex(index);
            }

            if (_comparer.Compare(Elements[biggerIndex], Elements[index]) < 0)
            {
                break;
            }

            Swap(biggerIndex, index);
            index = biggerIndex;
        }
    }

    private void ReCalculateUp()
    {
        var index = Elements.Count - 1;
        while (!IsRoot(index) && _comparer.Compare(Elements[index], GetParent(index)) > 0)
        {
            var parentIndex = GetParentIndex(index);
            Swap(parentIndex, index);
            index = parentIndex;
        }
    }

}
