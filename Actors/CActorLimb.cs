using UnityEngine;

public abstract class CActorLimb : CActor {

    public override void AddBaseActorProperties() {
        base.AddBaseActorProperties();

#if __DEBUG__
        //_oObj.Add("Dev_Limb_SlerpStrength",             this, 1, 0, 20);
        //_oObj.Add("Dev_Limb_SlerpDamper",               this, 1, 0, 20);
        ////_oObj.Add("Dev_Limb_RigidBody_Mass",            this, 1, 0, 10);
        //_oObj.Add("Dev_Limb_RigidBody_Drag",            this, 1, 0, 30);
        //_oObj.Add("Dev_Limb_RigidBody_AngDrag",         this, 1, 0, 30);
        //_oObj.Add("Dev_Pin_PosStrength",                this, 100, 0, 500);
        //_oObj.Add("Dev_Pin_PosDamper",                  this, 000, 0, 200);
        //_oObj.Add("Dev_Pin_RotStrength",                this, 5, 0, 500);
        //_oObj.Add("Dev_Pin_RotDamper",                  this, 0, 0, 200);
        //_oObj.Add("Dev_Pin_Mass",                       this, 1, 0.001f, 50);
        //_oObj.Add("Dev_Extremity_SlerpStrength",        this, 1, 0, 20);
        //_oObj.Add("Dev_Extremity_SlerpDamper",          this, 1, 0, 20);
        //_oObj.Add("Dev_Extremity_Mass",                 this, 1, 0.001f, 50);
#endif
    }

#if __DEBUG__
    //public void OnSet_Dev_Limb_SlerpStrength(float nValueOld, float nValueNew) {
    //    foreach (CBone oBone in _aBones)
    //        CUtility.Joint_SetSlerpPositionSpring(oBone._oJointD6, nValueNew);
    //}
    //public void OnSet_Dev_Limb_SlerpDamper(float nValueOld, float nValueNew) {
    //    foreach (CBone oBone in _aBones)
    //        CUtility.Joint_SetSlerpPositionDamper(oBone._oJointD6, nValueNew);
    //}
    //public void OnSet_Dev_Limb_RigidBody_Mass(float nValueOld, float nValueNew) {
    //    foreach (CBone oBone in _aBones)
    //        oBone.GetComponent<Rigidbody>().mass = nValueNew;
    //}
    //public void OnSet_Dev_Limb_RigidBody_Drag(float nValueOld, float nValueNew) {
    //    foreach (CBone oBone in _aBones)
    //        oBone.GetComponent<Rigidbody>().drag = nValueNew;
    //}
    //public void OnSet_Dev_Limb_RigidBody_AngDrag(float nValueOld, float nValueNew) {
    //    foreach (CBone oBone in _aBones)
    //        oBone.GetComponent<Rigidbody>().angularDrag = nValueNew;
    //}
    //public void OnSet_Dev_Pin_PosStrength(float nValueOld, float nValueNew) {
    //    SoftJointLimitSpring oLimitSpring = _oJoint_Extremity.linearLimitSpring;
    //    oLimitSpring.spring = nValueNew;
    //    _oJoint_Extremity.linearLimitSpring = oLimitSpring;
    //}
    //public void OnSet_Dev_Pin_PosDamper(float nValueOld, float nValueNew) {
    //    SoftJointLimitSpring oLimitSpring = _oJoint_Extremity.linearLimitSpring;
    //    oLimitSpring.damper = nValueNew;
    //    _oJoint_Extremity.linearLimitSpring = oLimitSpring;
    //}
    //public void OnSet_Dev_Pin_RotStrength(float nValueOld, float nValueNew) {
    //    SoftJointLimitSpring oLimitSpring = _oJoint_Extremity.angularXLimitSpring;
    //    oLimitSpring.spring = nValueNew;
    //    _oJoint_Extremity.angularXLimitSpring  = oLimitSpring;
    //    _oJoint_Extremity.angularYZLimitSpring = oLimitSpring;
    //}
    //public void OnSet_Dev_Pin_RotDamper(float nValueOld, float nValueNew) {
    //    SoftJointLimitSpring oLimitSpring = _oJoint_Extremity.angularXLimitSpring;
    //    oLimitSpring.damper = nValueNew;
    //    _oJoint_Extremity.angularXLimitSpring  = oLimitSpring;
    //    _oJoint_Extremity.angularYZLimitSpring = oLimitSpring;
    //}
    //public void OnSet_Dev_Pin_Mass(float nValueOld, float nValueNew) {
    //    GetComponent<Rigidbody>().mass = nValueNew;
    //}
    //public void OnSet_Dev_Extremity_SlerpStrength(float nValueOld, float nValueNew) {
    //    CUtility.Joint_SetSlerpPositionSpring(_oBoneExtremity._oJointD6, nValueNew);
    //}
    //public void OnSet_Dev_Extremity_SlerpDamper(float nValueOld, float nValueNew) {
    //    CUtility.Joint_SetSlerpPositionSpring(_oBoneExtremity._oJointD6, nValueNew);
    //}
    //public void OnSet_Dev_Extremity_Mass(float nValueOld, float nValueNew) {
    //    _oBoneExtremity.GetComponent<Rigidbody>().mass = nValueNew;
    //}
#endif
}
