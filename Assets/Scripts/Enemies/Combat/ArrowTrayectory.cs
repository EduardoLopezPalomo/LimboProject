using UnityEngine;

public class ArrowTrayectory : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Animator colorAnimator;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private int resolution = 30;
    
    private Transform originTransform;
    private Transform directionSource; 
    private float currentSpeed;
    private bool isShowing;
    

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.enabled = false;
    }

    public void Show(Transform transform, float speed, bool special)
    {
        this.originTransform = transform;
        this.directionSource = transform;
        this.currentSpeed = speed;
        isShowing = true;
        
        lineRenderer.enabled = true;
        lineRenderer.positionCount = resolution;
        if (special)
        {
            colorAnimator.SetTrigger("SpecialShowTrayectory");  
        }
        else
        {
            colorAnimator.SetTrigger("ShowTrayectory");   
        }
    }

    private void Update()
    {
        if (!isShowing) return;

        UpdatePoints();
    }
    
    private void UpdatePoints()
    {
        Vector3 origin = originTransform.position;
        Vector3 direction = directionSource.forward;
        direction.y = 0f; 
        direction.Normalize();
        Vector3 velocity = direction.normalized * currentSpeed;
        
        float totalTime = maxDistance / currentSpeed;
        float timeStep = totalTime / resolution;

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            Vector3 point = origin + velocity * t;
            lineRenderer.SetPosition(i, point);
        }
    }
    
    
    public void Hide()
    {
        isShowing = false;
        lineRenderer.enabled = false;
    }
}
