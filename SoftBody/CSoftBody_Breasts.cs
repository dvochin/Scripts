using UnityEngine;


public class CSoftBody_BreastL : CSoftBody {

	public override void Initialize(CBody oBody, int nSoftBodyID, Transform oBoneRootT) {
		base.Initialize(oBody, nSoftBodyID, oBoneRootT);

  //      //=== Create the managing object and related hotspot ===
		//_oObj = new CObj(this, "Breast", "Breast", _oBodyBase._oObj);
		//_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
  //      _oObj.Add(ESoftBodyBreast.BreastStiffness,			"Stiffness",		0.1f, 0.01f, 0.2f, "");     //###TUNE
		//_oObj.Add(ESoftBodyBreast.BreastSize,				"Size",				1.0f, 0.8f, 1.2f, "");		//###TUNE
		//CGame._oVrWandL ._oObjDebugJoystickHor_HACK = _oObj.Find(ESoftBodyBreast.Size);
	}

	public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		base.PreContainerUpdate(solver, cntr, parameters);
	}

	public override void OnPropChanged(CObj oObj) {               // This *must* be called within context of Flex's 'PreContainerUpdate()'
		base.OnPropChanged(oObj);
		switch (oObj._sName) {
			case "Stiffness":
				ShapeDef_SetStiffness(oObj._nValue);
				_oBody._oSoftBody_BreastR.ShapeDef_SetStiffness(oObj._nValue);
				break;

			case "Size":
				for (int nShapeIndex = 0; nShapeIndex < _oFlexShapeMatching.m_shapeIndicesCount; nShapeIndex++)
					_oFlexShapeMatching.m_shapeRestPositions[nShapeIndex] = _aShapeRestPositionsBAK[nShapeIndex] * oObj._nValue;
				for (int nShapeIndex = 0; nShapeIndex < _oBody._oSoftBody_BreastR._oFlexShapeMatching.m_shapeIndicesCount; nShapeIndex++)
					_oBody._oSoftBody_BreastR._oFlexShapeMatching.m_shapeRestPositions[nShapeIndex] = _oBody._oSoftBody_BreastR._aShapeRestPositionsBAK[nShapeIndex] * oObj._nValue;
				break;
		}
	}
};

public class CSoftBody_BreastR : CSoftBody { }        //###NOTE: Right breast is an empty subclass of CSoftBody.  Everything about the right breast is handled by CSoftBody_BreastL
