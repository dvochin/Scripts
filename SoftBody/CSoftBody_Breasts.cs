using UnityEngine;


public class CSoftBody_BreastL : CSoftBody {

	public override void Initialize(CBody oBody, int nSoftBodyID, Transform oBoneRootT) {
		base.Initialize(oBody, nSoftBodyID, oBoneRootT);

        //=== Create the managing object and related hotspot ===
		_oObj = new CObject(this, "Breast", "Breast");
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "Breast", typeof(ESoftBodyBreast));
        oPropGrp.PropAdd(ESoftBodyBreast.Stiffness,			"Stiffness",		0.1f, 0.01f, 0.2f, "");
        oPropGrp.PropAdd(ESoftBodyBreast.Size,				"Size",				1.0f, 0.5f, 1.3f, "");
		CGame.INSTANCE._oVrWandLeft ._oPropDebugJoystickHor_HACK = oPropGrp.PropFind(ESoftBodyBreast.Size);

		_oObj.FinishInitialization();
	}

	public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		base.PreContainerUpdate(solver, cntr, parameters);
	}

	public override void OnPropChanged(CProp oProp) {               // This *must* be called within context of Flex's 'PreContainerUpdate()'
		base.OnPropChanged(oProp);
		switch (oProp._nPropOrdinal) {
			case (int)ESoftBodyBreast.Stiffness:
				ShapeDef_SetStiffness(oProp._nValueLocal);
				_oBody._oSoftBody_BreastR.ShapeDef_SetStiffness(oProp._nValueLocal);
				break;

			case (int)ESoftBodyBreast.Size:
				for (int nShapeIndex = 0; nShapeIndex < _oFlexShapeMatching.m_shapeIndicesCount; nShapeIndex++)
					_oFlexShapeMatching.m_shapeRestPositions[nShapeIndex] = _aShapeRestPositionsBAK[nShapeIndex] * oProp._nValueLocal;
				for (int nShapeIndex = 0; nShapeIndex < _oBody._oSoftBody_BreastR._oFlexShapeMatching.m_shapeIndicesCount; nShapeIndex++)
					_oBody._oSoftBody_BreastR._oFlexShapeMatching.m_shapeRestPositions[nShapeIndex] = _oBody._oSoftBody_BreastR._aShapeRestPositionsBAK[nShapeIndex] * oProp._nValueLocal;
				break;
		}
	}
};

public class CSoftBody_BreastR : CSoftBody { }        //###NOTE: Right breast is an empty subclass of CSoftBody.  Everything about the right breast is handled by CSoftBody_BreastL
