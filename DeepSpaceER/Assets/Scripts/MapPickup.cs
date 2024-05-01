using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPickup : MonoBehaviour
{
    public Item[] pickupItems;
    public InventoryManager inventoryManager;
    public SpriteRenderer spriteRenderer;
    private bool isInRange;
    private bool isEmpty;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space) && isInRange) {
            if (!isEmpty) {
                bool result = inventoryManager.AddItem(pickupItems[0]);
                if (result == true) {
                    Debug.Log("Map added");
                }
                isEmpty = true;
                this.spriteRenderer.enabled = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        isInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other) {
        isInRange = false;
    }
}
