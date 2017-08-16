using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[ AddComponentMenu( "Physics/Throwable" ) ]
[ RequireComponent( typeof( Rigidbody ) ) ]
public class Throwable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	#region Public Fields

	[ Tooltip( "Sets a maximum throw force magnitude." ) ]
	[ SerializeField ] float m_MaximumForce = Mathf.Infinity;

	#endregion

	#region Private Fields

	Collider m_Collider;
	Bounds m_ColliderBounds;
	Rigidbody m_Rigidbody;
	List<ThrowFrame> m_ThrowFrames = new List<ThrowFrame>();
	const int m_MaximumThrowFramesCount = 4;

	#endregion

	#region Properties

	// Disable physics simulation when true.
	bool PointerControlled
	{
		set
		{
			m_Collider.enabled = !value;
			m_Rigidbody.isKinematic = value;
		}
	}

	#endregion

	#region Unity Callbacks

	#if UNITY_EDITOR

	// Add any needed objects in the current scene if they do not already exist.
	public void Reset()
	{
		UnityEditor.EditorApplication.ExecuteMenuItem( "GameObject/UI/Event System" );

		if( Camera.main == null )
		{
			UnityEditor.EditorApplication.ExecuteMenuItem( "GameObject/Camera" );
			Camera mainCamera = FindObjectOfType<Camera>();
			mainCamera.name = "Main Camera";
			mainCamera.tag = "MainCamera";
		}

		GameObject mainCameraGameObject = Camera.main.gameObject;
		if( mainCameraGameObject.GetComponent<PhysicsRaycaster>() == null )
		{
			mainCameraGameObject.AddComponent<PhysicsRaycaster>();
		}
	}

	#endif

	// Get references.
	public void Start()
	{
		m_Collider = GetComponent<Collider>();
		m_ColliderBounds = m_Collider.bounds;
		m_Rigidbody = GetComponent<Rigidbody>();
	}

	#endregion

	#region Unity Events

	// Switch to pointer control.
	public void OnPointerDown( PointerEventData _ )
	{
		PointerControlled = true;
	}

	// Switch to physics simulation.
	public void OnPointerUp( PointerEventData _ )
	{
		PointerControlled = false;
	}

	// Capture first frame of drag.
	public void OnBeginDrag( PointerEventData eventData )
	{
		PointerControlled = true;
		SetThrowFrame( eventData );
	}

	// Capture current frame of drag.
	public void OnDrag( PointerEventData eventData )
	{
		PointerControlled = true;
		SetThrowFrame( eventData );
	}

	// Capture final frame of drag and initiate the throw.
	public void OnEndDrag( PointerEventData eventData )
	{
		SetThrowFrame( eventData );
		PointerControlled = false;
		Throw();
	}

	#endregion

	#region Physics

	// Get throw calculation result and apply it to rigidbody.
	void Throw()
	{
		Vector3? force = ThrowForce();
		if( force.HasValue )
		{
			m_Rigidbody.AddForce( force.Value, ForceMode.VelocityChange );
		}

		m_ThrowFrames.Clear();
	}

	// Get new position of dragged object and store it along with the current time as a single frame of throw.
	void SetThrowFrame( PointerEventData eventData )
	{
		if( eventData.pointerEnter != null )
		{
			RaycastResult pointerCurrentRaycast = eventData.pointerCurrentRaycast;
			Vector3 raycastHitPosition = pointerCurrentRaycast.worldPosition;

			// Offset the raycast hit position so the throwable object will not intersect with the object hit by the raycast.
			Vector3 offset = pointerCurrentRaycast.worldNormal * m_ColliderBounds.extents.magnitude;
			Vector3 newFramePosition = raycastHitPosition + offset;

			// Actually move object to new offset position.
			transform.position = newFramePosition;

			// Store new position and time.
			m_ThrowFrames.Add( new ThrowFrame( newFramePosition, Time.time ) );

			// Limit number of stored frames.
			while( m_ThrowFrames.Count > m_MaximumThrowFramesCount )
			{
				m_ThrowFrames.RemoveAt( 0 );
			}
		}
	}

	// Calculate the amount of force that should be applied to the rigidbody based on the stored frames of the throw.
	Vector3? ThrowForce()
	{
		// The throw has to have at least start and end frames.
		if( m_ThrowFrames.Count < 2 )
		{
			return null;
		}

		// Get delta between first and last frames.
		ThrowFrame startFrame = m_ThrowFrames[ 0 ];
		ThrowFrame endFrame = m_ThrowFrames[ m_ThrowFrames.Count - 1 ];
		var delta = new ThrowFrame( endFrame.position - startFrame.position, endFrame.time - startFrame.time );

		// If the time elapsed in the throw is almost 0 then it doesn't count.
		if( Mathf.Approximately( delta.time, 0f ) )
		{
			return null;
		}

		Vector3 force = delta.position / delta.time;

		// Limit the force magnitude.
		if( force.magnitude > m_MaximumForce )
		{
			force.Normalize();
			force *= m_MaximumForce;
		}

		return force;
	}

	#endregion

	#region Helpers

	// Small struct to store position and time of an object during a throw.
	[System.Serializable]
	struct ThrowFrame
	{
		public Vector3 position;
		public float time;

		public ThrowFrame( Vector3 position, float time )
		{
			this.position = position;
			this.time = time;
		}
	}

	#endregion
}
