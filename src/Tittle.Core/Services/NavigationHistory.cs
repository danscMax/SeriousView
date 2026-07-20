using System.Collections.Generic;

namespace Tittle.Core.Services;

/// <summary>Back/forward navigation history over heading ordinals (ported Alt+←/→). A new jump records the
/// position being left and clears the forward stack; Back/Forward move between recorded positions, stashing
/// the current one on the opposite stack. Negatives (no active heading) are ignored so a bad ordinal can't
/// enter the history.</summary>
public sealed class NavigationHistory
{
    private readonly Stack<int> _back = new();
    private readonly Stack<int> _forward = new();

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
}
