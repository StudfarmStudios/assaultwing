/*
* Farseer Physics Engine based on Box2D.XNA port:
* Copyright (c) 2010 Ian Qvist
* 
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.gphysics.com 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.Diagnostics;
using FarseerPhysics.Common;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Dynamics.Joints
{
    /// <summary>
    /// A revolute joint rains to bodies to share a common point while they
    /// are free to rotate about the point. The relative rotation about the shared
    /// point is the joint angle. You can limit the relative rotation with
    /// a joint limit that specifies a lower and upper angle. You can use a motor
    /// to drive the relative rotation about the shared point. A maximum motor torque
    /// is provided so that infinite forces are not generated.
    /// </summary>
    public class RevoluteJoint : Joint
    {
        public Vector2 LocalAnchorA;

        public Vector2 LocalAnchorB;
        private bool _enableLimit;
        private bool _enableMotor;
        private Vector3 _impulse;
        private LimitState _limitState;
        private float _lowerAngle;
        private Mat33 _mass; // effective mass for point-to-point constraint.
        private float _maxMotorTorque;
        private float _motorImpulse;
        private float _motorMass; // effective mass for motor/limit angular constraint.
        private float _motorSpeed;
        private float _referenceAngle;
        private float _tmpFloat1;
        private Vector2 _tmpVector1, _tmpVector2;
        private float _upperAngle;

        internal RevoluteJoint()
        {
            JointType = JointType.Revolute;
        }

        /// <summary>
        /// Initialize the bodies and local anchor.
        /// This requires defining an
        /// anchor point where the bodies are joined. The definition
        /// uses local anchor points so that the initial configuration
        /// can violate the constraint slightly. You also need to
        /// specify the initial relative angle for joint limits. This
        /// helps when saving and loading a game.
        /// The local anchor points are measured from the body's origin
        /// rather than the center of mass because:
        /// 1. you might not know where the center of mass will be.
        /// 2. if you add/remove shapes from a body and recompute the mass,
        /// the joints will be broken.
        /// </summary>
        /// <param name="bodyA">The first body.</param>
        /// <param name="bodyB">The second body.</param>
        /// <param name="localAnchorA">The first body anchor.</param>
        /// <param name="localAnchorB">The second anchor.</param>
        public RevoluteJoint(Body bodyA, Body bodyB, Vector2 localAnchorA, Vector2 localAnchorB)
            : base(bodyA, bodyB)
        {
            JointType = JointType.Revolute;

            // Changed to local coordinates.
            LocalAnchorA = localAnchorA;
            LocalAnchorB = localAnchorB;

            ReferenceAngle = BodyB.Rotation - BodyA.Rotation;

            _impulse = Vector3.Zero;

            _limitState = LimitState.Inactive;
        }

        public override Vector2 WorldAnchorA
        {
            get { return BodyA.GetWorldPoint(LocalAnchorA); }
        }

        public override Vector2 WorldAnchorB
        {
            get { return BodyB.GetWorldPoint(LocalAnchorB); }
            set { Debug.Assert(false, "You can't set the world anchor on this joint type."); }
        }

        public float ReferenceAngle
        {
            get { return _referenceAngle; }
            set
            {
                WakeBodies();
                _referenceAngle = value;
            }
        }

        /// <summary>
        /// Get the current joint angle in radians.
        /// </summary>
        /// <value></value>
        public float JointAngle
        {
            get { return BodyB.Sweep.A - BodyA.Sweep.A - ReferenceAngle; }
        }

        /// <summary>
        /// Get the current joint angle speed in radians per second.
        /// </summary>
        /// <value></value>
        public float JointSpeed
        {
            get { return BodyB.AngularVelocityInternal - BodyA.AngularVelocityInternal; }
        }

        /// <summary>
        /// Is the joint limit enabled?
        /// </summary>
        /// <value><c>true</c> if [limit enabled]; otherwise, <c>false</c>.</value>
        public bool LimitEnabled
        {
            get { return _enableLimit; }
            set
            {
                WakeBodies();
                _enableLimit = value;
            }
        }

        /// <summary>
        /// Get the lower joint limit in radians.
        /// </summary>
        /// <value></value>
        public float LowerLimit
        {
            get { return _lowerAngle; }
            set
            {
                WakeBodies();
                _lowerAngle = value;
            }
        }

        /// <summary>
        /// Get the upper joint limit in radians.
        /// </summary>
        /// <value></value>
        public float UpperLimit
        {
            get { return _upperAngle; }
            set
            {
                WakeBodies();
                _upperAngle = value;
            }
        }

        /// <summary>
        /// Is the joint motor enabled?
        /// </summary>
        /// <value><c>true</c> if [motor enabled]; otherwise, <c>false</c>.</value>
        public bool MotorEnabled
        {
            get { return _enableMotor; }
            set
            {
                WakeBodies();
                _enableMotor = value;
            }
        }

        /// <summary>
        /// Set the motor speed in radians per second.
        /// </summary>
        /// <value>The speed.</value>
        public float MotorSpeed
        {
            set
            {
                WakeBodies();
                _motorSpeed = value;
            }
            get { return _motorSpeed; }
        }

        /// <summary>
        /// Set the maximum motor torque, usually in N-m.
        /// </summary>
        /// <value>The torque.</value>
        public float MaxMotorTorque
        {
            set
            {
                WakeBodies();
                _maxMotorTorque = value;
            }
            get { return _maxMotorTorque; }
        }

        /// <summary>
        /// Get the current motor torque, usually in N-m.
        /// </summary>
        /// <value></value>
        public float MotorTorque
        {
            get { return _motorImpulse; }
            set
            {
                WakeBodies();
                _motorImpulse = value;
            }
        }

        public override Vector2 GetReactionForce(float inv_dt)
        {
            Vector2 P = new Vector2(_impulse.X, _impulse.Y);
            return inv_dt * P;
        }

        public override float GetReactionTorque(float inv_dt)
        {
            return inv_dt * _impulse.Z;
        }

        internal override void InitVelocityConstraints(ref TimeStep step)
        {
            Body b1 = BodyA;
            Body b2 = BodyB;

            if (_enableMotor || _enableLimit)
            {
                // You cannot create a rotation limit between bodies that
                // both have fixed rotation.
                Debug.Assert(b1.InvI > 0.0f || b2.InvI > 0.0f);
            }

            // Compute the effective mass matrix.
            /*Transform xf1, xf2;
            b1.GetTransform(out xf1);
            b2.GetTransform(out xf2);*/

            Vector2 r1 = MathUtils.Multiply(ref b1.Xf.R, LocalAnchorA - b1.LocalCenter);
            Vector2 r2 = MathUtils.Multiply(ref b2.Xf.R, LocalAnchorB - b2.LocalCenter);

            // J = [-I -r1_skew I r2_skew]
            //     [ 0       -1 0       1]
            // r_skew = [-ry; rx]

            // Matlab
            // K = [ m1+r1y^2*i1+m2+r2y^2*i2,  -r1y*i1*r1x-r2y*i2*r2x,          -r1y*i1-r2y*i2]
            //     [  -r1y*i1*r1x-r2y*i2*r2x, m1+r1x^2*i1+m2+r2x^2*i2,           r1x*i1+r2x*i2]
            //     [          -r1y*i1-r2y*i2,           r1x*i1+r2x*i2,                   i1+i2]

            float m1 = b1.InvMass, m2 = b2.InvMass;
            float i1 = b1.InvI, i2 = b2.InvI;

            _mass.Col1.X = m1 + m2 + r1.Y * r1.Y * i1 + r2.Y * r2.Y * i2;
            _mass.Col2.X = -r1.Y * r1.X * i1 - r2.Y * r2.X * i2;
            _mass.Col3.X = -r1.Y * i1 - r2.Y * i2;
            _mass.Col1.Y = _mass.Col2.X;
            _mass.Col2.Y = m1 + m2 + r1.X * r1.X * i1 + r2.X * r2.X * i2;
            _mass.Col3.Y = r1.X * i1 + r2.X * i2;
            _mass.Col1.Z = _mass.Col3.X;
            _mass.Col2.Z = _mass.Col3.Y;
            _mass.Col3.Z = i1 + i2;

            _motorMass = i1 + i2;
            if (_motorMass > 0.0f)
            {
                _motorMass = 1.0f / _motorMass;
            }

            if (_enableMotor == false)
            {
                _motorImpulse = 0.0f;
            }

            if (_enableLimit)
            {
                float jointAngle = b2.Sweep.A - b1.Sweep.A - ReferenceAngle;
                if (Math.Abs(_upperAngle - _lowerAngle) < 2.0f * Settings.AngularSlop)
                {
                    _limitState = LimitState.Equal;
                }
                else if (jointAngle <= _lowerAngle)
                {
                    if (_limitState != LimitState.AtLower)
                    {
                        _impulse.Z = 0.0f;
                    }
                    _limitState = LimitState.AtLower;
                }
                else if (jointAngle >= _upperAngle)
                {
                    if (_limitState != LimitState.AtUpper)
                    {
                        _impulse.Z = 0.0f;
                    }
                    _limitState = LimitState.AtUpper;
                }
                else
                {
                    _limitState = LimitState.Inactive;
                    _impulse.Z = 0.0f;
                }
            }
            else
            {
                _limitState = LimitState.Inactive;
            }

            if (Settings.EnableWarmstarting)
            {
                // Scale impulses to support a variable time step.
                _impulse *= step.dtRatio;
                _motorImpulse *= step.dtRatio;

                Vector2 P = new Vector2(_impulse.X, _impulse.Y);

                b1.LinearVelocityInternal -= m1 * P;
                MathUtils.Cross(ref r1, ref P, out _tmpFloat1);
                b1.AngularVelocityInternal -= i1 * ( /* r1 x P */_tmpFloat1 + _motorImpulse + _impulse.Z);

                b2.LinearVelocityInternal += m2 * P;
                MathUtils.Cross(ref r2, ref P, out _tmpFloat1);
                b2.AngularVelocityInternal += i2 * ( /* r2 x P */_tmpFloat1 + _motorImpulse + _impulse.Z);
            }
            else
            {
                _impulse = Vector3.Zero;
                _motorImpulse = 0.0f;
            }
        }

        internal override void SolveVelocityConstraints(ref TimeStep step)
        {
            Body b1 = BodyA;
            Body b2 = BodyB;

            Vector2 v1 = b1.LinearVelocityInternal;
            float w1 = b1.AngularVelocityInternal;
            Vector2 v2 = b2.LinearVelocityInternal;
            float w2 = b2.AngularVelocityInternal;

            float m1 = b1.InvMass, m2 = b2.InvMass;
            float i1 = b1.InvI, i2 = b2.InvI;

            // Solve motor constraint.
            if (_enableMotor && _limitState != LimitState.Equal)
            {
                float Cdot = w2 - w1 - _motorSpeed;
                float impulse = _motorMass * (-Cdot);
                float oldImpulse = _motorImpulse;
                float maxImpulse = step.dt * _maxMotorTorque;
                _motorImpulse = MathUtils.Clamp(_motorImpulse + impulse, -maxImpulse, maxImpulse);
                impulse = _motorImpulse - oldImpulse;

                w1 -= i1 * impulse;
                w2 += i2 * impulse;
            }

            // Solve limit constraint.
            if (_enableLimit && _limitState != LimitState.Inactive)
            {
                /*Transform xf1, xf2;
                b1.GetTransform(out xf1);
                b2.GetTransform(out xf2);*/

                Vector2 r1 = MathUtils.Multiply(ref b1.Xf.R, LocalAnchorA - b1.LocalCenter);
                Vector2 r2 = MathUtils.Multiply(ref b2.Xf.R, LocalAnchorB - b2.LocalCenter);

                // Solve point-to-point constraint
                MathUtils.Cross(w2, ref r2, out _tmpVector2);
                MathUtils.Cross(w1, ref r1, out _tmpVector1);
                Vector2 Cdot1 = v2 + /* w2 x r2 */ _tmpVector2 - v1 - /* w1 x r1 */ _tmpVector1;
                float Cdot2 = w2 - w1;
                Vector3 Cdot = new Vector3(Cdot1.X, Cdot1.Y, Cdot2);

                Vector3 impulse = _mass.Solve33(-Cdot);

                if (_limitState == LimitState.Equal)
                {
                    _impulse += impulse;
                }
                else if (_limitState == LimitState.AtLower)
                {
                    float newImpulse = _impulse.Z + impulse.Z;
                    if (newImpulse < 0.0f)
                    {
                        Vector2 reduced = _mass.Solve22(-Cdot1);
                        impulse.X = reduced.X;
                        impulse.Y = reduced.Y;
                        impulse.Z = -_impulse.Z;
                        _impulse.X += reduced.X;
                        _impulse.Y += reduced.Y;
                        _impulse.Z = 0.0f;
                    }
                }
                else if (_limitState == LimitState.AtUpper)
                {
                    float newImpulse = _impulse.Z + impulse.Z;
                    if (newImpulse > 0.0f)
                    {
                        Vector2 reduced = _mass.Solve22(-Cdot1);
                        impulse.X = reduced.X;
                        impulse.Y = reduced.Y;
                        impulse.Z = -_impulse.Z;
                        _impulse.X += reduced.X;
                        _impulse.Y += reduced.Y;
                        _impulse.Z = 0.0f;
                    }
                }

                Vector2 P = new Vector2(impulse.X, impulse.Y);

                v1 -= m1 * P;
                MathUtils.Cross(ref r1, ref P, out _tmpFloat1);
                w1 -= i1 * ( /* r1 x P */_tmpFloat1 + impulse.Z);

                v2 += m2 * P;
                MathUtils.Cross(ref r2, ref P, out _tmpFloat1);
                w2 += i2 * ( /* r2 x P */_tmpFloat1 + impulse.Z);
            }
            else
            {
                /*Transform xf1, xf2;
                b1.GetTransform(out xf1);
                b2.GetTransform(out xf2);*/

                _tmpVector1 = LocalAnchorA - b1.LocalCenter;
                _tmpVector2 = LocalAnchorB - b2.LocalCenter;
                Vector2 r1 = MathUtils.Multiply(ref b1.Xf.R, ref _tmpVector1);
                Vector2 r2 = MathUtils.Multiply(ref b2.Xf.R, ref _tmpVector2);

                // Solve point-to-point constraint
                MathUtils.Cross(w2, ref r2, out _tmpVector2);
                MathUtils.Cross(w1, ref r1, out _tmpVector1);
                Vector2 Cdot = v2 + /* w2 x r2 */ _tmpVector2 - v1 - /* w1 x r1 */ _tmpVector1;
                Vector2 impulse = _mass.Solve22(-Cdot);

                _impulse.X += impulse.X;
                _impulse.Y += impulse.Y;

                v1 -= m1 * impulse;
                MathUtils.Cross(ref r1, ref impulse, out _tmpFloat1);
                w1 -= i1 * /* r1 x impulse */ _tmpFloat1;

                v2 += m2 * impulse;
                MathUtils.Cross(ref r2, ref impulse, out _tmpFloat1);
                w2 += i2 * /* r2 x impulse */ _tmpFloat1;
            }

            b1.LinearVelocityInternal = v1;
            b1.AngularVelocityInternal = w1;
            b2.LinearVelocityInternal = v2;
            b2.AngularVelocityInternal = w2;
        }

        internal override bool SolvePositionConstraints()
        {
            // TODO_ERIN block solve with limit. COME ON ERIN

            Body b1 = BodyA;
            Body b2 = BodyB;

            float angularError = 0.0f;
            float positionError;

            // Solve angular limit constraint.
            if (_enableLimit && _limitState != LimitState.Inactive)
            {
                float angle = b2.Sweep.A - b1.Sweep.A - ReferenceAngle;
                float limitImpulse = 0.0f;

                if (_limitState == LimitState.Equal)
                {
                    // Prevent large angular corrections
                    float C = MathUtils.Clamp(angle - _lowerAngle, -Settings.MaxAngularCorrection,
                                              Settings.MaxAngularCorrection);
                    limitImpulse = -_motorMass * C;
                    angularError = Math.Abs(C);
                }
                else if (_limitState == LimitState.AtLower)
                {
                    float C = angle - _lowerAngle;
                    angularError = -C;

                    // Prevent large angular corrections and allow some slop.
                    C = MathUtils.Clamp(C + Settings.AngularSlop, -Settings.MaxAngularCorrection, 0.0f);
                    limitImpulse = -_motorMass * C;
                }
                else if (_limitState == LimitState.AtUpper)
                {
                    float C = angle - _upperAngle;
                    angularError = C;

                    // Prevent large angular corrections and allow some slop.
                    C = MathUtils.Clamp(C - Settings.AngularSlop, 0.0f, Settings.MaxAngularCorrection);
                    limitImpulse = -_motorMass * C;
                }

                b1.Sweep.A -= b1.InvI * limitImpulse;
                b2.Sweep.A += b2.InvI * limitImpulse;

                b1.SynchronizeTransform();
                b2.SynchronizeTransform();
            }

            // Solve point-to-point constraint.
            {
                /*Transform xf1, xf2;
                b1.GetTransform(out xf1);
                b2.GetTransform(out xf2);*/

                Vector2 r1 = MathUtils.Multiply(ref b1.Xf.R, LocalAnchorA - b1.LocalCenter);
                Vector2 r2 = MathUtils.Multiply(ref b2.Xf.R, LocalAnchorB - b2.LocalCenter);

                Vector2 C = b2.Sweep.C + r2 - b1.Sweep.C - r1;
                positionError = C.Length();

                float invMass1 = b1.InvMass, invMass2 = b2.InvMass;
                float invI1 = b1.InvI, invI2 = b2.InvI;

                // Handle large detachment.
                const float k_allowedStretch = 10.0f * Settings.LinearSlop;
                if (C.LengthSquared() > k_allowedStretch * k_allowedStretch)
                {
                    // Use a particle solution (no rotation).
                    Vector2 u = C;
                    u.Normalize();
                    float k = invMass1 + invMass2;
                    Debug.Assert(k > Settings.Epsilon);
                    float m = 1.0f / k;
                    Vector2 impulse2 = m * (-C);
                    const float k_beta = 0.5f;
                    b1.Sweep.C -= k_beta * invMass1 * impulse2;
                    b2.Sweep.C += k_beta * invMass2 * impulse2;

                    C = b2.Sweep.C + r2 - b1.Sweep.C - r1;
                }

                Mat22 K1 = new Mat22(new Vector2(invMass1 + invMass2, 0.0f), new Vector2(0.0f, invMass1 + invMass2));
                Mat22 K2 = new Mat22(new Vector2(invI1 * r1.Y * r1.Y, -invI1 * r1.X * r1.Y),
                                     new Vector2(-invI1 * r1.X * r1.Y, invI1 * r1.X * r1.X));
                Mat22 K3 = new Mat22(new Vector2(invI2 * r2.Y * r2.Y, -invI2 * r2.X * r2.Y),
                                     new Vector2(-invI2 * r2.X * r2.Y, invI2 * r2.X * r2.X));

                Mat22 Ka;
                Mat22.Add(ref K1, ref K2, out Ka);

                Mat22 K;
                Mat22.Add(ref Ka, ref K3, out K);


                Vector2 impulse = K.Solve(-C);

                b1.Sweep.C -= b1.InvMass * impulse;
                MathUtils.Cross(ref r1, ref impulse, out _tmpFloat1);
                b1.Sweep.A -= b1.InvI * /* r1 x impulse */ _tmpFloat1;

                b2.Sweep.C += b2.InvMass * impulse;
                MathUtils.Cross(ref r2, ref impulse, out _tmpFloat1);
                b2.Sweep.A += b2.InvI * /* r2 x impulse */ _tmpFloat1;

                b1.SynchronizeTransform();
                b2.SynchronizeTransform();
            }

            return positionError <= Settings.LinearSlop && angularError <= Settings.AngularSlop;
        }
    }
}
