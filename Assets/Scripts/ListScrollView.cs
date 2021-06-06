using System;
using System.Collections.Generic;
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
public class ListScrollView: MonoBehaviour 
{
    [SerializeField] private float elementsInView = 5;
    [SerializeField] private float padding = 5;
    [SerializeField] private int bufferSize = 1;
    [SerializeField] private GameObject elementPrefab;

    private IListScollViewController infoScript;
    private int elementCount;
    private GameObject[] elements;
    private RectTransform content;
    private int currentPosition = 0; //The ID of the top element
    private int arrayPosition = 0;
    private float elementBufferHeight;

    private void Awake()
    {
        content = transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
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
        if(elementsToPlace > elementCount)
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
            element.GetComponent<LayoutElement>().preferredHeight = elementHeight;

            elements[i] = element;

            //TODO fill element with data
            infoScript.FillWithInfo(element, i);
        }

        //Resize the content
        content.sizeDelta = new Vector2(content.sizeDelta.x, (elementBufferHeight) * elementCount);

        return elements;
    }
    private void CheckBounds(Vector2 position)
    {
        float height = content.transform.localPosition.y + elementBufferHeight;
        int newPosition = Mathf.FloorToInt(height / elementBufferHeight);
        //if the index has changed
        if (currentPosition != newPosition)
        {
            ShiftElements(newPosition);
        }
    }
    private void ShiftElements(int newPosition)
    {
        int delta = Mathf.Abs(currentPosition - newPosition);
        //For each element to shift
        for (int i = 0; i < delta; i++)
        {
            bool isUp = currentPosition > newPosition;
            if (isUp) ShiftUp();
            else ShiftDown();
            currentPosition += isUp ? -1 : 1;
        }
    }
    private void ShiftUp()
    {
        if (IsOOB())
        {
            return;
        }
        Debug.Log("shifting UP");

        //Get bottom element
        GameObject toShift = elements[ClampToArraySize(arrayPosition + elements.Length - 1, elements.Length)];
        float toShiftHeight = elements[arrayPosition].transform.localPosition.y + elementBufferHeight;

        toShift.transform.localPosition = new Vector2(toShift.transform.localPosition.x, toShiftHeight);
        infoScript.FillWithInfo(toShift, currentPosition-1);

        arrayPosition = ClampToArraySize(arrayPosition - 1, elements.Length);
    }
    private void ShiftDown()
    {
        if (IsOOB())
        {
            return;
        }
        Debug.Log("shifting DOWN");

        //Get top element
        GameObject toShift = elements[arrayPosition];
        float toShiftHeight = elements[ClampToArraySize(arrayPosition + elements.Length - 1, elements.Length)].transform.localPosition.y - elementBufferHeight;

        toShift.transform.localPosition = new Vector2(toShift.transform.localPosition.x, toShiftHeight);
        infoScript.FillWithInfo(toShift, currentPosition+elements.Length-1);

        arrayPosition = ClampToArraySize(arrayPosition + 1, elements.Length);
    }
    private bool IsOOB()
    {
        return currentPosition <= 0 || currentPosition + Mathf.CeilToInt(elementsInView) + bufferSize >= elementCount;
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