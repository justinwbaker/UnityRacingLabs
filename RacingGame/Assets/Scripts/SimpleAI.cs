using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAI : MonoBehaviour {

    public Transform waypointContainer;

    private Transform[] waypoints;
    private int currentWaypoint = 0;

    private float m_horizontalInput;
    private float m_verticalInput;
    private float m_handbrakeInput;
    private float m_steeringAngle;

    private float gearSpread;

    private bool m_handBreakOn;

    private Rigidbody body;

    public WheelCollider frontLeftW, frontRightW;
    public WheelCollider rearLeftW, rearRightW;

    public Transform frontLeftT, frontRightT;
    public Transform rearLeftT, rearRightT;

    public float maxSterringAngle = 30;
    public float motorForce = 50;
    public float handbreakForce = 100;
    public float handbrakeForwardSlip = 0.04f;
    public float handbrakeSidewaysSlip = 0.08f;
    public float maxSpeed = 150;
    public float breakingDistance = 6f;
    public float forwardOffset;

    public int numberOfGears = 6;

    public GameObject brakeLight;
    public Material idleLightMat;
    public Material brakeLightMat;
    public Material reverseLightMat;

    public void Start()
    {
        gearSpread = maxSpeed / numberOfGears;
        body = GetComponent<Rigidbody>();
        getWaypoints();
    }

    public void getInput()
    {
        Vector3 RelativeWaypointPosition = transform.InverseTransformPoint(new Vector3(waypoints[currentWaypoint].position.x, transform.position.y, waypoints[currentWaypoint].position.z));
        m_horizontalInput = RelativeWaypointPosition.x / RelativeWaypointPosition.magnitude;

        if (Mathf.Abs(m_horizontalInput) < 0.8f)
        {
            //when making minor turning adjustments speed is based on how far to the next point.
            m_verticalInput = (RelativeWaypointPosition.z / RelativeWaypointPosition.magnitude);
            m_handbrakeInput = 0;
        }
        else
        {
            if (body.velocity.magnitude > 4)
            {
                m_handbrakeInput = 1;
            }
            //if not moving forward backup and turn opposite.
            else if (body.velocity.z < 0)
            {
                m_handbrakeInput = 0;
                m_verticalInput = -1;
                m_horizontalInput *= -1;
            }
            //let off the gas while making a hard turn.
            else
            {
                m_handbrakeInput = 0;
                m_verticalInput = 0;
            }
        }
    }

    private void checkWaypoints()
    {
        Vector3 RelativeWaypointPosition = transform.InverseTransformPoint(new Vector3(waypoints[currentWaypoint].position.x, transform.position.y, waypoints[currentWaypoint].position.z));

        if (RelativeWaypointPosition.magnitude < 5)
        {
            currentWaypoint++;
            if (currentWaypoint >= waypoints.Length)
            {
                currentWaypoint = 0;
            }
        }
    }

    private void Steer()
    {
        m_steeringAngle = maxSterringAngle * m_horizontalInput;
        frontLeftW.steerAngle = m_steeringAngle;
        frontRightW.steerAngle = m_steeringAngle;
    }

    void SetSlipValues(float forward, float sideways)
    {
        //Change the stiffness values of wheel friction curve and then reapply it.
        WheelFrictionCurve tempStruct = rearRightW.forwardFriction;
        tempStruct.stiffness = forward;
        rearRightW.forwardFriction = tempStruct;

        tempStruct = rearRightW.sidewaysFriction;
        tempStruct.stiffness = sideways;
        rearRightW.sidewaysFriction = tempStruct;

        tempStruct = rearLeftW.forwardFriction;
        tempStruct.stiffness = forward;
        rearLeftW.forwardFriction = tempStruct;

        tempStruct = rearLeftW.sidewaysFriction;
        tempStruct.stiffness = sideways;
        rearLeftW.sidewaysFriction = tempStruct;
    }

    private void Break()
    {
        if (m_handbrakeInput > 0f)
        {
            m_handBreakOn = true;
            frontLeftW.brakeTorque = handbreakForce;
            frontRightW.brakeTorque = handbreakForce;

            //Wheels are locked, so power slide!
            if (GetComponent<Rigidbody>().velocity.magnitude > 1)
            {
                SetSlipValues(handbrakeForwardSlip, handbrakeSidewaysSlip);
            }
            else //skid to a stop, regular friction enabled.
            {
                SetSlipValues(1f, 1f);
            }
        }
        else
        {
            m_handBreakOn = false;
            frontLeftW.brakeTorque = 0;
            frontRightW.brakeTorque = 0;
            SetSlipValues(1f, 1f);
        }
    }

    private float ForwardRayCast()
    {
        RaycastHit hit;
        Vector3 carFront = transform.position + (transform.forward * forwardOffset);
        Debug.DrawRay(carFront, transform.forward * breakingDistance);

        //if we detect a car infront of us, slow down or even reverse based on distance.
        if (Physics.Raycast(carFront, transform.forward, out hit, breakingDistance))
        {
            return (((carFront - hit.point).magnitude / breakingDistance) * 2) - 1;
        }

        //otherwise no change
        return 1f;
    }

    private void Accelerate()
    {
        if (!m_handBreakOn)
        {
            float adjustment = ForwardRayCast();

            frontLeftW.motorTorque = adjustment * m_verticalInput * motorForce;
            frontRightW.motorTorque = adjustment * m_verticalInput * motorForce;
        }
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontLeftW, frontLeftT);
        UpdateWheelPose(frontRightW, frontRightT);
        UpdateWheelPose(rearLeftW, rearLeftT);
        UpdateWheelPose(rearRightW, rearRightT);
    }

    private void UpdateWheelPose(WheelCollider _collider, Transform _transform)
    {
        Vector3 _pos = _transform.position;
        Quaternion _quat = _transform.rotation;

        _collider.GetWorldPose(out _pos, out _quat);

        _transform.position = _pos;
        _transform.rotation = _quat;
    }

    private void DetermineLightState()
    {
        if (m_handBreakOn && GetComponent<Rigidbody>().velocity.magnitude > 0)
        {
            brakeLight.GetComponent<Renderer>().material = brakeLightMat;
        }
        else if (GetComponent<Rigidbody>().velocity.magnitude > 0 && m_verticalInput < 0)
        {
            brakeLight.GetComponent<Renderer>().material = reverseLightMat;
        }
        else
        {
            brakeLight.GetComponent<Renderer>().material = idleLightMat;
        }

    }

    private void EngineSound()
    {
        float currentSpeed = Mathf.Abs(GetComponent<Rigidbody>().velocity.z);
        if (currentSpeed > 0)
        {
            if (currentSpeed > maxSpeed)
            {
                GetComponent<AudioSource>().pitch = 1.75f;
            }
            else
            {
                GetComponent<AudioSource>().pitch = ((currentSpeed % gearSpread) / gearSpread) + 0.75f;
            }
        }
        //when reversing we have only one gear.
        else
        {
            GetComponent<AudioSource>().pitch = (currentSpeed / maxSpeed) + 0.75f;
        }

    }

    private void getWaypoints()
    {
        Transform[] potentialWaypoints = waypointContainer.GetComponentsInChildren<Transform>();
        waypoints = new Transform[potentialWaypoints.Length - 1];

        for (int i = 1; i < potentialWaypoints.Length; i++)
        {
            waypoints[i - 1] = potentialWaypoints[i];
        }
    }

    private void FixedUpdate()
    {
        getInput();
        checkWaypoints();
        Steer();
        Break();
        Accelerate();
        UpdateWheelPoses();
        DetermineLightState();
    }

    private void Update()
    {
        EngineSound();
    }

    public Transform GetCurrentWaypoint()
    {
        return waypoints[currentWaypoint];
    }

    public Transform GetLastWaypoint()
    {
        return waypoints[currentWaypoint - 1];
    }

    private void OnDrawGizmos()
    {
        if (waypoints.Length > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(waypoints[currentWaypoint].position, 2f);
        }
    }
}
