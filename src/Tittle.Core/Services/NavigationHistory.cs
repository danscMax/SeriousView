using System.Collections.Generic;

namespace Tittle.Core.Services;

/// <summary>Back/forward navigation history over heading ordinals (ported Alt+←/→). A new jump records the
/// position being left and clears the forward stack; Back/Forward move between recorded positions, stashing
/// the current one on the opposite stack. Negatives (no active heading) are ignored so a bad ordinal can't
/// enter the history. Both stacks are capped at <see cref="MaxDepth"/>; when full, the OLDEST entry is
/// silently dropped (outline clicks / unread jumps could otherwise grow unbounded over a long session).</summary>
public sealed class NavigationHistory
{
    private const int MaxDepth = 128;

    private readonly BoundedStack _back = new(MaxDepth);
    private readonly BoundedStack _forward = new(MaxDepth);

    public bool CanBack => _back.Count > 0;
    public bool CanForward => _forward.Count > 0;

    /// <summary>Record the position being left before a NEW (non-back/forward) jump. Clears the forward
    /// stack (a new branch); ignores a negative position.</summary>
    public void Record(int from)
    {
        if (from < 0)
            return;
        _back.Push(from);
        _forward.Clear();
    }

    /// <summary>Go back: stash <paramref name="current"/> on the forward stack and return the previous
    /// position, or null when there is nothing to go back to.</summary>
    public int? Back(int current)
    {
        if (_back.Count == 0)
            return null;
        if (current >= 0)
            _forward.Push(current);
        return _back.Pop();
    }

    /// <summary>Go forward: stash <paramref name="current"/> on the back stack and return the next
    /// position, or null when there is nothing to go forward to.</summary>
    public int? Forward(int current)
    {
        if (_forward.Count == 0)
            return null;
        if (current >= 0)
            _back.Push(current);
        return _forward.Pop();
    }

    // Test seams (InternalsVisibleTo Tittle.Tests): current depth of each stack.
    internal int BackCountForTests => _back.Count;
    internal int ForwardCountForTests => _forward.Count;

    /// <summary>Bounded LIFO of ints backed by a fixed ring array of the given capacity: pushing onto a
    /// full stack overwrites the slot holding the OLDEST entry, so the depth never exceeds the capacity.
    /// Push/Pop/Clear semantics otherwise match <see cref="Stack{T}"/>. Internal for the cap test seam.</summary>
    internal sealed class BoundedStack
    {
        private readonly int[] _items;
        private int _head; // index one past the top element (next push slot)
        private int _count;

        public BoundedStack(int capacity) => _items = new int[capacity];

        public int Count => _count;

        public void Push(int value)
        {
            _items[_head] = value;
            _head = (_head + 1) % _items.Length;
            if (_count < _items.Length)
                _count++;
            // else: full — the push overwrote the oldest entry in place.
        }

        public int Pop()
        {
            _head = (_head - 1 + _items.Length) % _items.Length; // non-negative: capacity ≥ 1
            _count--;
            return _items[_head];
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
