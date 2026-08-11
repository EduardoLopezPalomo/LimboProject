using UnityEngine;

public enum SlotType
{
   Close,
   Mid,
   Far
}
public class Slot 
{
   public Vector3 position;
   public GameObject reservedBy;
   
   public Slot(Vector3 position)
   {
      this.position = position;
   }
   
   public bool IsAvailable()
   {
      return (reservedBy == null);
   }
   
   public float GetAngle(Vector3 playerPos)
   {
      Vector3 offset = position - playerPos;
      return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
   }
}
