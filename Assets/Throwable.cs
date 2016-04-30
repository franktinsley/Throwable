using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[ AddComponentMenu( "Physics/Throwable" ) ]
[ RequireComponent( typeof( Rigidbody ) ) ]
public class Throwable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	#region Public Fields

	[ Tooltip( "Sets a maximum throw force magnitude." ) ]
	public float m_MaximumForce = Mathf.Infinity;

	#endregion

	#region Private Fields

	Collider m_Collider;
	Bounds m_ColliderBounds;
	Rigidbody m_Rigidbody;
	List<ThrowFrame> m_ThrowFrames = new List<ThrowFrame>();
	const int m_MaximumThrowFramesCount = 4;

	#endregion

	#region Properties

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

	public void Start()
	{
		m_Collider = GetComponent<Collider>();
		m_ColliderBounds = m_Collider.bounds;
		m_Rigidbody = GetComponent<Rigidbody>();
	}

	#endregion

	#region Unity Events

	public void OnPointerDown( PointerEventData _ )
	{
		PointerControlled = true;
	}

	public void OnPointerUp( PointerEventData _ )
	{
		PointerControlled = false;
	}

	public void OnBeginDrag( PointerEventData eventData )
	{
		PointerControlled = true;
		SetThrowFrame( eventData );
	}

	public void OnDrag( PointerEventData eventData )
	{
		PointerControlled = true;
		SetThrowFrame( eventData );
	}

	public void OnEndDrag( PointerEventData eventData )
	{
		SetThrowFrame( eventData );
		PointerControlled = false;
		Throw();
	}

	#endregion

	#region Physics

	void Throw()
	{
		Vector3? force = ThrowForce();
		if( force.HasValue )
		{
			m_Rigidbody.AddForce( force.Value, ForceMode.VelocityChange );
		}

		m_ThrowFrames.Clear();
	}
	
	void SetThrowFrame( PointerEventData eventData )
	{
		if( eventData.pointerEnter != null )
		{
			RaycastResult pointerCurrentRaycast = eventData.pointerCurrentRaycast;

			Vector3 raycastHitPosition = pointerCurrentRaycast.worldPosition;
			Vector3 offset = pointerCurrentRaycast.worldNormal * m_ColliderBounds.extents.magnitude;
			Vector3 newFramePosition = raycastHitPosition + offset;

			transform.position = newFramePosition;

			m_ThrowFrames.Add( new ThrowFrame( newFramePosition, Time.time ) );
			while( m_ThrowFrames.Count > m_MaximumThrowFramesCount )
			{
				m_ThrowFrames.RemoveAt( 0 );
			}
		}
	}

	Vector3? ThrowForce()
	{
		if( m_ThrowFrames.Count < 2 )
		{
			return null;
		}

		ThrowFrame startFrame = m_ThrowFrames[ 0 ];
		ThrowFrame endFrame = m_ThrowFrames[ m_ThrowFrames.Count - 1 ];
		var delta = new ThrowFrame( endFrame.position - startFrame.position, endFrame.time - startFrame.time );

		if( Mathf.Approximately( delta.time, 0f ) )
		{
			return null;
		}

		Vector3 force = delta.position / delta.time;

		if( force.magnitude > m_MaximumForce )
		{
			force.Normalize();
			force *= m_MaximumForce;
		}

		return force;
	}

	#endregion

	#region Helpers

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
