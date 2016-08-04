//###SOURCE: Based on: https://unity3d.com/learn/tutorials/modules/intermediate/live-training-archive/panels-panes-windows?playlist=17111

using UnityEngine;
using UnityEngine.EventSystems;

public class CUIPanelDrag_OBS : MonoBehaviour, IPointerDownHandler, IDragHandler        // CUIDragPanel: Enables a user to drags a Unity-GUI panel window       ###OBS if we keep panel stacking??
{
    public RectTransform _oRectTranCanvas;
    public RectTransform _oRectTranPanel;
    public Vector2 _oVecOffsetOrigDragPoint;
    //public Vector2 _oVecPtrMouseDown;
    //public Vector2 _oVecPtrMouseDrag;

    void Awake()
    {
        CUIPanel oPanel = GetComponentInParent<CUIPanel>();               // Walk up the chain to find panel (this component attached to panel's title object which is a couple levels down)
        CUICanvas oCanvas = GetComponentInParent<CUICanvas>();
        //CUICanvas oCanvas = oUIPanel.transform.parent.GetComponent<CUICanvas>();   // Canvas is immediate parent of panel
        _oRectTranCanvas = oCanvas.transform as RectTransform;
        _oRectTranPanel = oPanel.transform as RectTransform;
    }

    public void OnPointerDown(PointerEventData data)        //###LEARN: How to easily trap mouse events!
    {
        _oRectTranPanel.SetAsLastSibling();
        //_oVecPtrMouseDown = data.position;
        //Debug.LogWarningFormat("Panel Down: {0}", _oVecPtrMouseDown.ToString());
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_oRectTranPanel, data.position, data.pressEventCamera, out _oVecOffsetOrigDragPoint);
    }

    public void OnDrag(PointerEventData data)
    {
        //if (_oRectTranPanel == null)
        //    return;
        //_oVecPtrMouseDrag = data.position;
        //Debug.LogWarningFormat("Panel Drag: {0}", _oVecPtrMouseDrag.ToString());
        //Vector2 pointerPostion = ClampToWindow(data);
        Vector2 pointerPostion = data.position;
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_oRectTranCanvas, pointerPostion, data.pressEventCamera, out localPointerPosition))     //###LEARN: Converting to point-in-rectangle!
            _oRectTranPanel.localPosition = localPointerPosition - _oVecOffsetOrigDragPoint;
    }

    //Vector2 ClampToWindow(PointerEventData data)      //###OBS?  Treat as infinite plane? ###BUG: User can irrecoverably move a panel away from canvas!  ###U
    //{
    //    Vector2 rawPointerPosition = data.position;

    //    Vector3[] canvasCorners = new Vector3[4];
    //    _oRectTranCanvas.GetWorldCorners(canvasCorners);

    //    float clampedX = Mathf.Clamp(rawPointerPosition.x, canvasCorners[0].x, canvasCorners[2].x);     // Corner 0 = lower left, Corner 2 = upper right
    //    float clampedY = Mathf.Clamp(rawPointerPosition.y, canvasCorners[0].y, canvasCorners[2].y);

    //    Vector2 newPointerPosition = new Vector2(clampedX, clampedY);
    //    return newPointerPosition;
    //}
}
