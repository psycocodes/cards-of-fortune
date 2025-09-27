using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;

/// <summary>
/// WebGL Context Manager - Handles WebGL context loss and prevents "Illegal view ID" errors
/// Place this script on a GameObject in your scene to manage WebGL context issues.
/// </summary>
public class WebGLContextManager : MonoBehaviourPun
{
    [Header("Context Recovery Settings")]
    public bool enableAutoReload = true;
    public float contextCheckInterval = 2.0f;
    
    [Header("Error Recovery UI")]
    public GameObject errorPanel;
    public TextMeshProUGUI errorMessageText;
    public Button reloadButton;
    
    private bool contextLost = false;
    private bool isChecking = false;
    
    // Singleton instance
    public static WebGLContextManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Only run in WebGL builds
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            StartCoroutine(MonitorWebGLContext());
            SetupErrorRecoveryUI();
        }
    }
    
    void SetupErrorRecoveryUI()
    {
        // Create error recovery UI if not provided
        if (errorPanel == null)
        {
            CreateErrorRecoveryUI();
        }
        
        if (reloadButton != null)
        {
            reloadButton.onClick.AddListener(ReloadPage);
        }
        
        // Hide error panel initially
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }
    
    void CreateErrorRecoveryUI()
    {
        // Create a simple error recovery UI
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            var canvasObj = new GameObject("ErrorCanvas");
            canvas = canvasObj;
            var canvasComp = canvas.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComp.sortingOrder = 1000;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
        }
        
        // Create error panel
        errorPanel = new GameObject("ErrorPanel");
        errorPanel.transform.SetParent(canvas.transform, false);
        
        var panelImage = errorPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        var rect = errorPanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Create error text
        var textObj = new GameObject("ErrorText");
        textObj.transform.SetParent(errorPanel.transform, false);
        
        errorMessageText = textObj.AddComponent<TextMeshProUGUI>();
        errorMessageText.text = "Graphics Error: The game needs to reload due to WebGL context loss.";
        errorMessageText.fontSize = 24;
        errorMessageText.color = Color.white;
        errorMessageText.alignment = TextAlignmentOptions.Center;
        
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.6f);
        textRect.anchorMax = new Vector2(0.9f, 0.8f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Create reload button
        var buttonObj = new GameObject("ReloadButton");
        buttonObj.transform.SetParent(errorPanel.transform, false);
        
        reloadButton = buttonObj.AddComponent<Button>();
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        
        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.4f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.6f, 0.5f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // Button text
        var buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        
        var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "RELOAD GAME";
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
    }
    
    IEnumerator MonitorWebGLContext()
    {
        while (true)
        {
            yield return new WaitForSeconds(contextCheckInterval);
            
            if (!isChecking && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                CheckWebGLContext();
            }
        }
    }
    
    void CheckWebGLContext()
    {
        isChecking = true;
        
        try
        {
            // Try to perform a simple graphics operation that would fail if context is lost
            var testTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            testTexture.SetPixel(0, 0, Color.white);
            testTexture.Apply();
            DestroyImmediate(testTexture);
            
            // If we get here, context is likely OK
            contextLost = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"WebGL Context Check Failed: {ex.Message}");
            HandleContextLoss();
        }
        
        isChecking = false;
    }
    
    void HandleContextLoss()
    {
        if (contextLost) return; // Already handled
        
        contextLost = true;
        Debug.LogError("WebGL Context Lost! Attempting recovery...");
        
        // Stop all RPC calls and network operations
        if (PhotonNetwork.IsConnected)
        {
            try
            {
                PhotonNetwork.Disconnect();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error disconnecting from Photon: {ex.Message}");
            }
        }
        
        // Show error UI
        ShowErrorRecoveryUI();
        
        // Auto-reload if enabled
        if (enableAutoReload)
        {
            StartCoroutine(AutoReloadAfterDelay(3.0f));
        }
    }
    
    void ShowErrorRecoveryUI()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
        }
        
        if (errorMessageText != null)
        {
            errorMessageText.text = "Graphics Error: WebGL context lost. The game needs to reload.";
        }
    }
    
    IEnumerator AutoReloadAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ReloadPage();
    }
    
    public void ReloadPage()
    {
        Debug.Log("Reloading page due to WebGL context loss...");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        // Use JavaScript to reload the page
        Application.ExternalEval("window.location.reload();");
#else
        Debug.LogWarning("Page reload only works in WebGL builds");
#endif
    }
    
    /// <summary>
    /// Safe wrapper for UI updates that checks for context loss
    /// Use this instead of direct UI updates in your PlayerController scripts
    /// </summary>
    /// <param name="updateAction">The UI update action to perform</param>
    /// <returns>True if update was successful, false if context is lost</returns>
    public static bool SafeUIUpdate(System.Action updateAction)
    {
        if (Instance != null && Instance.contextLost)
        {
            Debug.LogWarning("Skipping UI update - WebGL context lost");
            return false;
        }
        
        try
        {
            updateAction?.Invoke();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"UI Update failed (possible context loss): {ex.Message}");
            
            if (Instance != null)
            {
                Instance.HandleContextLoss();
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Safe wrapper for Photon RPC calls that checks for context loss
    /// </summary>
    /// <param name="photonView">The PhotonView to send RPC from</param>
    /// <param name="methodName">The RPC method name</param>
    /// <param name="target">The RPC target</param>
    /// <param name="parameters">The RPC parameters</param>
    /// <returns>True if RPC was sent successfully</returns>
    public static bool SafeRPC(PhotonView photonView, string methodName, RpcTarget target, params object[] parameters)
    {
        if (Instance != null && Instance.contextLost)
        {
            Debug.LogWarning($"Skipping RPC {methodName} - WebGL context lost");
            return false;
        }
        
        if (photonView == null || !PhotonNetwork.IsConnected)
        {
            Debug.LogWarning($"Cannot send RPC {methodName} - PhotonView null or not connected");
            return false;
        }
        
        try
        {
            photonView.RPC(methodName, target, parameters);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RPC {methodName} failed: {ex.Message}");
            
            // Check if this might be a context loss error
            if (ex.Message.Contains("view") || ex.Message.Contains("View") || ex.Message.Contains("ID"))
            {
                if (Instance != null)
                {
                    Instance.HandleContextLoss();
                }
            }
            
            return false;
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // Handle context loss when browser tab becomes inactive/active
        if (Application.platform == RuntimePlatform.WebGLPlayer && !pauseStatus)
        {
            // Tab became active again - check context
            StartCoroutine(CheckContextAfterDelay(1.0f));
        }
    }
    
    IEnumerator CheckContextAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        CheckWebGLContext();
    }
    
    void OnDestroy()
    {
        if (reloadButton != null)
        {
            reloadButton.onClick.RemoveAllListeners();
        }
    }
}