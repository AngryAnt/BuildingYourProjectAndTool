using UnityEngine;
using System.Collections;


public class Test : MonoBehaviour
{
	public float random;
	public float sinus;


	void Update ()
	{
		random = Random.Range (-1.0f, 1.0f);
		sinus = Mathf.Sin (Time.time);
	}
}
