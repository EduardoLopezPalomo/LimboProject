using System;
using UnityEngine;

public class ShieldMaskController : MonoBehaviour
{
   private Animator animator;
   private void Awake()
   {
      animator = gameObject.GetComponent<Animator>();
   }

   public void EnableShieldMask()
   {
      animator.SetLayerWeight(3,1f);
   }

   public void DisableShieldMask()
   {
      animator.SetLayerWeight(3,0f);
   }
}
