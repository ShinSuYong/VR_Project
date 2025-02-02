using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum HandObjectState { NON_OBJECT, OBJECT }
public class CameraRayToInteraction : SH.Singleton<CameraRayToInteraction>
{
    [SerializeField] public Image imageA;
    [SerializeField] public Image imageX;
    [SerializeField] public Camera mainCam;
    [SerializeField] public Camera subCam;
    [SerializeField] private Transform uiPoint;

    private HandObjectState curObjectState = HandObjectState.NON_OBJECT;

    [SerializeField]
    private LayerMask layerMask;

    [SerializeField]
    private LayerMask UIObjectLayer;

    [HideInInspector] public string TargetLayerName;

    private bool isInteracting = false;
    private bool isCloseUping = false;

    private GameObject curUIObject;
    private Item curObject;
    RaycastHit hit;
    private void Awake()
    {
        imageA.enabled = false;
        imageX.enabled = false;
        mainCam.enabled = true;
        subCam.enabled = false;
    }
    private void Update()
    {
        SetPosition();

        RaycastInteraction();
    }
    void SetPosition()
    {
        subCam.transform.position = mainCam.transform.position;
        subCam.transform.rotation = Quaternion.Euler(mainCam.transform.rotation.eulerAngles.x, mainCam.transform.rotation.eulerAngles.y, mainCam.transform.rotation.eulerAngles.z);
        transform.position = mainCam.transform.position;
        transform.rotation = mainCam.transform.rotation;

        Vector3 position = subCam.transform.position;
        position.y -= 0.5f;
        Vector3 dir = subCam.transform.forward;
        dir.y = 0;
        uiPoint.position = position + dir;
    }


    private void RaycastInteraction()
    {
        // 레이는 항상 쏨.
        Physics.Raycast(transform.position, transform.forward, out hit, 5f);

        Debug.DrawRay(transform.position, transform.forward * 5f, Color.red);

        // 타겟을 정해줌.
        SetHoverObject();

        // 타겟의 상태에 따라 어떤 작업을 할건가 결정.
        InteractionAndCloseUpKeyDown();

        // 타겟과의 상호작용.
        Interaction();


    }

    void SetHoverObject()
    {
        // 상호작용 하지 않을 때만 타겟을 정해준다.  
        // 손에 오브젝트를 들고 있을 때도 타겟을 정해주지 않는다.
        if (isCloseUping || isInteracting || curObjectState == HandObjectState.OBJECT)
            return;

        if (!hit.transform || hit.transform.gameObject.layer 
            != LayerMask.NameToLayer(GimmickManager.Instance.TargetLayerName))
        {
            
            curObject?.GetItemComponent<Hoverable>().HoverOff();
            curObject = null;
            return;
        }

        Item targetObject = null;

        if (!(targetObject = hit.transform.GetComponent<Item>()))
            return;

        if (!targetObject.IsOption(ItemOption.HOVER))
            return;


        if (targetObject == curObject)
            return;

        curObject?.GetItemComponent<Hoverable>().HoverOff();
        curObject = targetObject;
        curObject?.GetItemComponent<Hoverable>().HoverOn();
    }

    void InteractionAndCloseUpKeyDown()
    {
        // 타겟이 없거나 이미 상호작용 중이면 들어오지 않음.
        if (!(imageA.enabled = (curObject || isCloseUping || isInteracting)))
            return;

        // 클로즈업 가능한가?
        imageA.enabled = curObject.IsOption(ItemOption.CLOSEUP);

        // 키를 눌러서 상호작용
        if (curObject.IsOption(ItemOption.INTERACTION) && curObject && Input.GetMouseButtonUp(0))
        {
            curObject?.GetItemComponent<Hoverable>().HoverOff();
            curObject.GetItemComponent<SH.Interactionable>().Interaction();
            isInteracting = true;
        }

        // 키를 눌러서 클로즈업.
        if (imageA.enabled == true && Input.GetKeyDown(KeyCode.A))
        {
            curObject?.GetItemComponent<Hoverable>().HoverOff();
            curObject.GetItemComponent<CloseUpable>().CloseUp();
            ObjectCreate();
            MainView();
            imageA.enabled = false;
            imageX.enabled = true;
            isCloseUping = true;
        }
    }

    void Interaction()
    {
        // 상호작용 진행 중..
        if (isInteracting)
        {        
            isInteracting = curObject.GetItemComponent<SH.Interactionable>().InteractionUpdate();

            // 내부에서 끝나거나 키를 누르면 상호작용 종료
            if (!isInteracting || Input.GetKeyDown(KeyCode.X))
            {
                curObject.GetItemComponent<SH.Interactionable>().UnInteraction();
                InteractionEnd();
            }
        }

        // 클로즈업 진행 중..
        if (isCloseUping)
        {
            curObject.GetItemComponent<CloseUpable>().CloseUp();

            // X키를 누르면 클로즈업 종료.
            if (Input.GetKeyDown(KeyCode.X))
            {
                curObject.GetItemComponent<CloseUpable>().UnCloseUp();
                InteractionEnd();
                isCloseUping = false;
            }
        }
    }

    private void InteractionEnd()
    {
        
        SubView();
        Destroy(curUIObject);
        curUIObject = null;
        imageX.enabled = false;
        curObject = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
    }

    public void MainView()
    {
        mainCam.enabled = false;
        subCam.enabled = true;
    }
    public void SubView()
    {
        mainCam.enabled = true;
        subCam.enabled = false;
    }

    void ObjectCreate()
    {
        curUIObject = Instantiate(curObject.transform.gameObject, uiPoint.position, uiPoint.rotation);
        curUIObject.AddComponent<ObjectRotate>();
        
        //if (curObject.CloseUpLayer >= 0)
        //    curUIObject.layer = curObject.CloseUpLayer;
        //else
        //    Debug.LogError(curObject.name + "의 CloseUpLayer 테그가 현재 레이어 태그에 없습니다");
        
        
        if (curUIObject.GetComponent<Rigidbody>())
            curUIObject.GetComponent<Rigidbody>().useGravity = false;
    }
}