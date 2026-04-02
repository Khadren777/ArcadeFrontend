using System.Collections.Generic;
using System.Windows.Input;

/// <summary>
/// Handles hidden input sequences (cabinet-friendly version later).
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class SecretSequenceService
{
    private readonly Queue<Key> _buffer = new();
    private readonly Key[] _targetSequence;

    public SecretSequenceService(Key[] sequence)
    {
        _targetSequence = sequence;
    }

    public bool Register(Key key)
    {
        _buffer.Enqueue(key);

        if (_buffer.Count > _targetSequence.Length)
            _buffer.Dequeue();

        return Matches();
    }

    private bool Matches()
    {
        if (_buffer.Count != _targetSequence.Length)
            return false;

        int i = 0;
        foreach (var key in _buffer)
        {
            if (key != _targetSequence[i])
                return false;

            i++;
        }

        return true;
    }
}
