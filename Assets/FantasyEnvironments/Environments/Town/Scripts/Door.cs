using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour {
	public Animator anim;

	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();
	}

	void OnTriggerEnter (Collider other) {
		anim.SetBool ("DoorOpen", true);
		anim.SetBool ("DoorClose", false);

	}

	void OnTriggerExit (Collider other) {
		anim.SetBool ("DoorOpen", false);
		anim.SetBool ("DoorClose", true);

	}

	// Update is called once per frame
	void Update () {
		
	}
}
