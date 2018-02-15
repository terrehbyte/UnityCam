using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomOverlap : MonoBehaviour
{
	public bool isOverlapping = false;
	Collider attachedCollider;

	void Start()
	{
		attachedCollider = GetComponent<Collider>();
	}

	void Update()
	{
        Bounds colliderWorldBounds = attachedCollider.bounds;        
        Collider[] candidates = Physics.OverlapBox(colliderWorldBounds.center, colliderWorldBounds.extents, Quaternion.identity);

        List<Collider> overlapping = new List<Collider>();
        foreach(var testee in candidates)
        {
            Vector3 mtvDir;
            float mtvDist;

            // let A be the player
            // let B be the Collider we're testing against
            bool overlap = Physics.ComputePenetration(attachedCollider, transform.position, transform.rotation,
                                                      testee, testee.transform.position, testee.transform.rotation,
                                                      out mtvDir, out mtvDist );

            if (overlap && testee != attachedCollider)
            {
                overlapping.Add(testee);
            }
        }

		isOverlapping = overlapping.Count > 0;
	}
}
