using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro;

public class SceneChangeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string sceneToLoad = "Illusion 2";
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f);
    public float fadeSpeed = 0.1f;
    
    private Button button;
    private Image buttonImage;
    private Color originalColor;
    private TextMeshProUGUI buttonText;

    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        
        originalColor = buttonImage.color;
        button.onClick.AddListener(ChangeScene);
    }

    void ChangeScene()
    {
        StartCoroutine(ClickAnimation());
        //LevelManager.Instance.LoadScene(sceneToLoad);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonImage.color = originalColor;
    }

    System.Collections.IEnumerator ClickAnimation()
    {
        transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ChangeScene);
        }
    }
}