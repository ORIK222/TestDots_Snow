using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConnection
{
    void Abort();
    bool IsCompleted { get; }
    bool IsSuccess { get; }
    bool IsAborted { get; }
    string Error { get; }
    string Message { get; }
    ResponseData ResponseData { get; }
}
