using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public interface IListScollViewController
{
    public void FillWithInfo(GameObject toFill, int index);
}
/// <summary>
/// A class that builds a scroll view based on a finite list
/// It also recycles the elements in the list
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ListScrollView : MonoBehaviour
{
    [SerializeField] private float elementsInView = 5;
    [SerializeField] private float padding = 5;
    [SerializeField] private int bufferSize = 1;
    [SerializeField] private float previewScaleSpeed = 0.2f;
    [SerializeField] private GameObject elementPrefab;

    private IListScollViewController infoScript;
    private int elementCount;
    private GameObject[] elements;
    private RectTransform content;
    private int currentPosition = 0; //The ID of the top element
    private int arrayPosition = 0;
    private float elementBufferHeight;
    private RectTransform rectTransform;
    private RectTransform viewport;
    private RectTransform previewModal;

    private float bottomEdge, topEdge, oldPosition, previewModalReturnPos;

    private void Awake()
    {
        content = transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
        rectTransform = GetComponent<RectTransform>();
        viewport = transform.GetChild(0).GetComponent<RectTransform>();
        previewModal = transform.GetChild(0).GetChild(1).gameObject.GetComponent<RectTransform>();
        previewModal.GetComponent<Button>().onClick.AddListener(HidePreviewModal);
        GetComponent<ScrollRect>().onValueChanged.AddListener(CheckBounds);
    }

    public GameObject[] InitElements(IListScollViewController infoScript, int elementCount)
    {
        this.infoScript = infoScript;
        this.elementCount = elementCount;
        //Get rid off old elements
        if (elements != null)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                Destroy(elements[i]);
            }
        }
        //Gets the amount of prefabs to place, all in view + top/bottom buffer
        int elementsToPlace = Mathf.CeilToInt(elementsInView) + bufferSize + bufferSize;
        elements = new GameObject[elementsToPlace];
        float elementHeight = GetComponent<RectTransform>().rect.height / Mathf.CeilToInt(elementsInView);
        elementBufferHeight = elementHeight + padding;
        float baseOffset = -(elementHeight / 2 + padding);

        //Check if there arent too many elements
        if (elementsToPlace > elementCount)
        {
            elementsToPlace = elementCount;
        }

        for (int i = 0; i < elementsToPlace; i++)
        {
            GameObject element = Instantiate(elementPrefab);
            element.transform.SetParent(content);
            element.transform.localPosition = new Vector2(0, baseOffset - (elementBufferHeight) * i);
            element.transform.localScale = Vector3.one;
            RectTransform rt = element.GetComponent<RectTransform>();
            rt.offsetMax = new Vector2(0, rt.offsetMax.y);
            rt.offsetMin = new Vector2(0, rt.offsetMin.y);
            rt.sizeDelta = new Vector2(0, elementHeight);
            int index = i;
            element.GetComponent<Button>().onClick.AddListener(() => ShowPreviewModal(index, index));

            elements[i] = element;

            //TODO fill element with data
            infoScript.FillWithInfo(element, i);
        }

        bottomEdge = rectTransform.position.y + rectTransform.rect.yMin * transform.root.localScale.y - elementBufferHeight * bufferSize;
        topEdge = rectTransform.position.y + rectTransform.rect.yMax * transform.root.localScale.y + elementBufferHeight * bufferSize;

        //Resize the content
        content.sizeDelta = new Vector2(content.sizeDelta.x, (elementBufferHeight) * elementCount);
        content.transform.position = new Vector3(content.transform.position.x, 0, 0);
        arrayPosition = 0;
        currentPosition = 0;

        return elements;
    }
    public void ShowPreviewModal(int arrayIndex, int positionIndex)
    {
        infoScript.FillWithInfo(previewModal.gameObject, positionIndex); //Set info
        previewModal.gameObject.SetActive(true); //Enable object
        previewModal.transform.position = elements[arrayIndex].transform.position; //Set position to element position
        previewModalReturnPos = elements[arrayIndex].transform.position.y; //Set return position for when closing
        StartCoroutine(ScalePreviewModal(elementBufferHeight, viewport.rect.height, transform.position.y, false));
    }
    public void HidePreviewModal()
    {
        StartCoroutine(ScalePreviewModal(viewport.rect.height, elementBufferHeight, previewModalReturnPos, true));
    }
    private IEnumerator ScalePreviewModal(float startHeight, float endHeight, float targetHeightPos, bool toggleAtEnd)
    {
        float t = 0;
        float startHeightPos = previewModal.transform.position.y;
        while (previewModal.rect.height != endHeight)
        {
            t += Time.deltaTime / previewScaleSpeed;
            previewModal.sizeDelta = new Vector2(previewModal.sizeDelta.x, Mathf.Lerp(startHeight, endHeight, t));
            float heightPos = Mathf.Lerp(startHeightPos, targetHeightPos, t);
            previewModal.transform.position = new Vector3(previewModal.transform.position.x, heightPos, 0);
            yield return null;
        }
        if (toggleAtEnd)
            previewModal.gameObject.SetActive(false);
    }
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawLine(new Vector3(0, bottomEdge, 0), new Vector3(1000, bottomEdge, 0));
    //    Gizmos.DrawLine(new Vector3(0, topEdge, 0), new Vector3(1000, topEdge, 0));
    //    Gizmos.DrawCube(rectTransform.transform.position, Vector3.one*50);
    //}
    private void CheckBounds(Vector2 position)
    {
        //float height = content.transform.localPosition.y + elementBufferHeight;
        //currentPosition = Mathf.FloorToInt(height / elementBufferHeight);
        //arrayPosition = currentPosition % elements.Length;
        //if (currentPosition == newPosition) return;
        bool isUp = oldPosition > content.transform.localPosition.y;
        oldPosition = content.transform.localPosition.y;
        if (isUp)
        {
            //While bottom element is too low
            while (elements[ClampToArraySize(arrayPosition + elements.Length - 1, elements.Length)].transform.position.y < bottomEdge)
            {
                if (IsAtTop())
                {
                    return;
                }
                ShiftUp();
            }
        }
        else
        {
            //While top element is too high
            while (elements[arrayPosition].transform.position.y > topEdge)
            {
                if (IsAtBottom())
                {
                    return;
                }
                ShiftDown();
            }
        }
    }
    private void ShiftUp()
    {
        Debug.Log("shifting UP");

        //Get bottom element
        int localArrayIndex = ClampToArraySize(arrayPosition + elements.Length - 1, elements.Length);
        GameObject toShift = elements[localArrayIndex];
        float toShiftHeight = elements[arrayPosition].transform.localPosition.y + elementBufferHeight;

        toShift.transform.localPosition = new Vector2(toShift.transform.localPosition.x, toShiftHeight);
        toShift.GetComponent<Button>().onClick.RemoveAllListeners();
        int localPositionIndex = currentPosition - 1;
        toShift.GetComponent<Button>().onClick.AddListener(() => ShowPreviewModal(ClampToArraySize(localArrayIndex, elements.Length), localPositionIndex));
        infoScript.FillWithInfo(toShift, localPositionIndex);


        arrayPosition = ClampToArraySize(arrayPosition - 1, elements.Length);
        currentPosition--;
    }
    private void ShiftDown()
    {
        Debug.Log("shifting DOWN");

        //Get top element
        int localArrayIndex = arrayPosition;
        GameObject toShift = elements[localArrayIndex];
        GameObject toShiftBelow = elements[ClampToArraySize(arrayPosition + elements.Length - 1, elements.Length)];
        float toShiftHeight = toShiftBelow.transform.localPosition.y - elementBufferHeight;

        toShift.transform.localPosition = new Vector2(toShift.transform.localPosition.x, toShiftHeight);
        toShift.GetComponent<Button>().onClick.RemoveAllListeners();
        int localPositionIndex = currentPosition + elements.Length;
        toShift.GetComponent<Button>().onClick.AddListener(() => ShowPreviewModal(localArrayIndex, localPositionIndex));
        infoScript.FillWithInfo(toShift, localPositionIndex);

        arrayPosition = ClampToArraySize(arrayPosition + 1, elements.Length);
        currentPosition++;
    }
    private bool IsOOB()
    {
        return currentPosition <= 0 || currentPosition + Mathf.CeilToInt(elementsInView) + bufferSize >= elementCount;
    }
    private bool IsAtTop()
    {
        return currentPosition <= 0;
    }
    private bool IsAtBottom()
    {
        return currentPosition + Mathf.CeilToInt(elementsInView) + bufferSize * 2 >= elementCount;
    }
    private int ClampToArraySize(int unclamped, int length)
    {
        while (unclamped >= length)
        {
            unclamped -= length;
        }
        while (unclamped < 0)
        {
            unclamped += length;
        }
        return unclamped;
    }
}