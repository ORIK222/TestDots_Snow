using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TiltBrushToolkit;
using UnityEngine;
using static TiltBrushToolkit.ImportGltf;

/// <summary>
/// Asynhronious controller.
/// </summary>
public class AsyncController : MonoBehaviour
{
    // This is to sync controllers and execute them sequentally. Later we may remove controllers synchronization if they do not cause freezing in VR.
    private static List<AsyncController> controllers = new List<AsyncController>();

    private List<Action> actionsForMainThread = new List<Action>();
    private Exception exception;
    private Func<AsyncController, IEnumerator> coroutine;

    public GltfImportOptions Options
    {
        get;
        set;
    }

    public GltfImportResult Result
    {
        get;
        set;
    }

    public IUriLoader UriLoader
    {
        get;
        set;
    }

    public ImportState State
    {
        get;
        set;
    }

    public Action<AsyncController> Success
    {
        get;
        set;
    }

    public Action<Exception> Failure
    {
        get;
        set;
    }

    /// <summary>
    /// Executes the coroutine on main thread.
    /// </summary>
    /// <param name="coroutine">The coroutine.</param>
    /// <remarks>May be called from background thread.</remarks>
    public void ExecuteCoroutine(Func<AsyncController, IEnumerator> coroutine)
    {
        this.coroutine = coroutine;
    }

    /// <summary>
    /// Finishes async execution.
    /// </summary>
    /// <remarks>Important! Should be called from main thread only.</remarks>
    public void Finish()
    {
        try
        {
            this.Result.root.SetActive(true);
            Success?.Invoke(this);
        }
        finally
        {
            DestroyMyself();
        }
    }

    /// <summary>
    /// Causes to finish processing due to exception.
    /// </summary>
    /// <param name="exc">The exception that occured.</param>
    /// <remarks>
    /// The method may be called from backgorund thread.
    /// </remarks>
    public void ProcessingFailure(Exception exc)
    {
        this.exception = exc;
    }

    /// <summary>
    /// Executes action on the main thread asynchroniously.
    /// </summary>
    /// <param name="action">The action.</param>
    public void ExecuteOnMainThreadAsync(Action action)
    {
        actionsForMainThread.Add(action);
    }

    /// <summary>
    /// Executes action on the main thread synchroniously.
    /// </summary>
    /// <param name="action">The action.</param>
    public void ExecuteOnMainThreadSync(Action action)
    {
        ExecuteOnMainThreadAsync(action);
        while (actionsForMainThread.Count > 0 && actionsForMainThread.Contains(action))
        {
            Thread.Sleep(50);
        }
    }

    private void Start()
    {
        controllers.Add(this);
    }

    private void Update()
    {
        if (controllers.IndexOf(this) == 0)
        {
            if (coroutine != null)
            {
                var seq = coroutine;
                coroutine = null;
                StartCoroutine(seq(this));
            }

            if (actionsForMainThread.Count > 0)
            {
                foreach (Action action in actionsForMainThread)
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception exc)
                    {
                        Debug.LogError("Exception during execution of callback action: " + exc);
                    }
                }

                actionsForMainThread.Clear();
            }

            if (exception != null && this.Failure != null)
            {
                try
                {
                    Failure.Invoke(this.exception);
                }
                finally
                {
                    DestroyMyself();
                }
            }
        }
    }

    public void DestroyMyself()
    {
        Failure = null;
        Success = null;
        actionsForMainThread = null;
        exception = null;
        coroutine = null;
        if (Result != null)
        {
            if (Result.materialCollector is IDisposable)
            {
                (Result.materialCollector as IDisposable).Dispose();
            }

            Result.materials = null;
            Result.materialCollector = null;
            Result.meshes = null;
            Result.root = null;
            Result.textures = null;
            Result = null;
        }

        if (this.UriLoader is IDisposable)
        {
            (this.UriLoader as IDisposable).Dispose();
        }

        this.UriLoader = null;
        if (this.State != null)
        {
            this.State.Dispose();
            this.State = null;
        }

        controllers.Remove(this);
        Destroy(this.gameObject);
    }
}
