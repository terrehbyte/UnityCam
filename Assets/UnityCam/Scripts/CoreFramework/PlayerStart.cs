using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStart : MonoBehaviour
{
	void OnDrawGizmos()
	{
		Gizmos.DrawIcon(transform.position, UnityCamStatics.gizmoPathRoot + "player.png", true);
	}
}
