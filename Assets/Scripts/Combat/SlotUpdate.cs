using UnityEngine;

public class SlotUpdate : MonoBehaviour
{
    public float timeToUpdate = 10f;
    private float timer;
    private bool canUpdate = true;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= timeToUpdate)
        {
            canUpdate = true;
            timer = 0f;
        }
    }

    public bool CanUpdateSlot()
    {
        if (canUpdate)
        {
            canUpdate = false;
            return true;
        }
        return false;
    }

}


