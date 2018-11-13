using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCarController : MonoBehaviour {

    private float m_horizontalInput;
    private float m_verticalInput;
    private float m_handbrakeInput;
    private float m_steeringAngle;

    private float gearSpread;
    
    private bool m_handBreakOn;

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

    public int numberOfGears = 6;

    public GameObject brakeLight;
    public Material idleLightMat;
    public Material brakeLightMat;
    public Material reverseLightMat;

    public void Start()
    {
        gearSpread = maxSpeed / numberOfGears;
    }

    public void getInput()
    {
        m_horizontalInput = Input.GetAxis("Horizontal");
        m_verticalInput = Input.GetAxis("Vertical");
        m_handbrakeInput = Input.GetAxisRaw("Handbreak");
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

    private void Accelerate()
    {
        if (!m_handBreakOn) { 
            frontLeftW.motorTorque = m_verticalInput * motorForce;
            frontRightW.motorTorque = m_verticalInput * motorForce;
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
        if (m_handBreakOn && GetComponent<Rigidbody>().velocity.magnitude>0)
        {
            brakeLight.GetComponent<Renderer>().material = brakeLightMat;
        }
        else if(GetComponent<Rigidbody>().velocity.magnitude > 0 && m_verticalInput < 0)
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

    private void FixedUpdate()
    {
        getInput();
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
}
