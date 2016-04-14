using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[ AddComponentMenu( "Physics/Throwable" ) ]
[ RequireComponent( typeof( Rigidbody ) ) ]
public class Throwable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	#region Public Fields

	public float maxThrowMagnitude = Mathf.Infinity;

	#endregion

	#region Private Fields

	Collider m_Collider;
	Bounds m_ColliderBounds;
	Rigidbody m_Rigidbody;
	readonly List<ThrowFrame> m_ThrowFrames = new List<ThrowFrame>();
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

	void Start()
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
		Vector3 force;
		if( ThrowForce( out force ) )
		{
			m_Rigidbody.AddForce( force, ForceMode.VelocityChange );
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
			if( m_ThrowFrames.Count > m_MaximumThrowFramesCount )
			{
				m_ThrowFrames.RemoveAt( 0 );
			}
		}
	}

	bool ThrowForce( out Vector3 force )
	{
		force = Vector3.zero;

		if( m_ThrowFrames.Count < 2 )
		{
			return false;
		}

		ThrowFrame startFrame = m_ThrowFrames[ 0 ];
		ThrowFrame endFrame = m_ThrowFrames[ m_ThrowFrames.Count - 1 ];
		var delta = new ThrowFrame( endFrame.position - startFrame.position, endFrame.time - startFrame.time );

		if( Mathf.Approximately( delta.time, 0f ) )
		{
			return false;
		}

		force = delta.position / delta.time;

		if( force.magnitude > maxThrowMagnitude )
		{
			force.Normalize();
			force *= maxThrowMagnitude;
		}

		return true;
	}

	#endregion

	#region Helpers

	struct ThrowFrame
	{
		public Vector3 position;
		public readonly float time;

		public ThrowFrame( Vector3 position, float time )
		{
			this.position = position;
			this.time = time;
		}
	}

	#endregion
}
