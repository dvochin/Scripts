using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


//---------------------------------------------------------------------------	
public class ErosEngine {		//###DESIGN: Is there really a need for CGame???


	[HideInInspector]	public const string ErosDll = "ErosEngine";			// The filename of the ErosEngine C++ dll that performs physics simulation, clothing, soft bodies, fluid and interface to Blender

	////---------------------------------------------------------------------------	PHYSX3
	//[DllImport(ErosDll)] public static extern void		PhysX3_Create();
	//[DllImport(ErosDll)] public static extern void		PhysX3_Destroy();
	//[DllImport(ErosDll)] public static extern int		PhysX3_SimulateFrame(float nTimeDelta);

	////---------------------------------------------------------------------------	PHYSX2 (SOFTBODY)
	//[DllImport(ErosDll)] public static extern void		PhysX2_Create();
	//[DllImport(ErosDll)] public static extern void		PhysX2_Destroy();
	//[DllImport(ErosDll)] public static extern void		PhysX2_SimulateFrame(float nTimeDelta);

	//---------------------------------------------------------------------------	OBJECT
	[DllImport(ErosDll)] public static extern int		Object_GoOnline(IntPtr hObject, IntPtr pInitBuf);		//###DESIGN!!: Revisit decision behind that 'misc buffer'!!
	[DllImport(ErosDll)] public static extern int		Object_GoOffline(IntPtr hObject);
	[DllImport(ErosDll)] public static extern int		Object_PropConnect(IntPtr hObject, int nPropEnumOrdinal, string sNameProp);
	[DllImport(ErosDll)] public static extern int		Object_PropDestroy(IntPtr hObject, int nPropEnumOrdinal);
	[DllImport(ErosDll)] public static extern float		Object_PropGet(IntPtr hProp, int nPropEnumOrdinal);
	[DllImport(ErosDll)] public static extern float		Object_PropSet(IntPtr hProp, int nPropEnumOrdinal, float nValue);
	[DllImport(ErosDll)] public static extern void		Object_SetPositionOrientation(IntPtr hObject, int nSubObject, Vector3 vecPos, Quaternion quatRot);

	////---------------------------------------------------------------------------	FLUID
	//[DllImport(ErosDll)] public static extern IntPtr	Fluid_Create (string sNameObject, int nNumProps);
	//[DllImport(ErosDll)] public static extern void		Fluid_Destroy(IntPtr pFluidPtr);
	//[DllImport(ErosDll)] public static extern int		Fluid_Polygonize(IntPtr pFluidPtr);
	//[DllImport(ErosDll)] public static extern IntPtr	Fluid_Polygonize_GetTriangles(IntPtr pFluidPtr, IntPtr aTrisOut, int nTriIndices);
	//[DllImport(ErosDll)] public static extern IntPtr	Fluid_Polygonize_GetVertices (IntPtr pFluidPtr, IntPtr aVertsOut, int nVerts);
	//[DllImport(ErosDll)] public static extern void		MCube_Init();
	//[DllImport(ErosDll)] public static extern void		MCube_Destroy();
	////---------------------------------------------------------------------------	CLOTH
	//[DllImport(ErosDll)] public static extern IntPtr	Cloth_Create(string sNameObject, int nNumProps, int nVerts, IntPtr aVertsIntPtr, int nTris, IntPtr aTris, IntPtr aMapClothVertsSimToSkin, int nMapClothVertsSimToSkin, float nClothInitStretch, float nClothInitStretchFirstFrame);
	//[DllImport(ErosDll)] public static extern void		Cloth_Destroy(IntPtr hCloth);
	//[DllImport(ErosDll)] public static extern void		Cloth_OnSimulatePre (IntPtr hCloth, IntPtr pBodyColClothPtr, IntPtr aVertsSkinnedCloth);
	//[DllImport(ErosDll)] public static extern void		Cloth_OnSimulatePost(IntPtr hCloth);
	//[DllImport(ErosDll)] public static extern void		Cloth_Reset(IntPtr hCloth);
	//[DllImport(ErosDll)] public static extern void		Cloth_ConnectClothToBreastColliders(IntPtr pClothPtr, IntPtr pBodyColBreastLPtr, IntPtr pBodyColBreastRPtr);

 //   //---------------------------------------------------------------------------	SOFTBODY
	//[DllImport(ErosDll)] public static extern IntPtr	SoftBody_Create(string szNameObject, int nNumProps, IntPtr pVerts, int nVerts, IntPtr pTris, int nTris, IntPtr pNormals, int nTetraMeshDetailLevel, float nDensity, bool bCollisionSelf, bool bCollisionTwoWay, int eColGroup);
	//[DllImport(ErosDll)] public static extern void		SoftBody_Destroy(IntPtr pSoftBodyPtr);
	//[DllImport(ErosDll)] public static extern int		SoftBody_GetTetraTri(IntPtr pSoftBodyPtr, int nTetraIndex);		// Accesses inside the tetramesh triangle index to find and constuct CPinTetras
	//[DllImport(ErosDll)] public static extern Vector3	SoftBody_GetParticle(IntPtr pSoftBodyPtr, int nVertTetra);		// Accesses inside the tetramesh vertices to find and constuct CPinTetras
	//[DllImport(ErosDll)] public static extern int		SoftBody_GetParticleCount(IntPtr pSoftBodyPtr);					// Accesses inside the tetramesh vertices to find number of particles
	//[DllImport(ErosDll)] public static extern void		SoftBody_Breasts_UpdateCBodyColBreast(IntPtr pBodyColBreastPtr, IntPtr pVerts, IntPtr pNormals, float nRadiusSphereBase, float nOutsideProtusion, int eColGroup);
	//[DllImport(ErosDll)] public static extern int 		PinTetra_AttachParticleToPos(IntPtr pSoftBodyPtr, int nVertTetra, Vector3 vecPinPos);		//####TODO: Rename?
	
	////---------------------------------------------------------------------------	COLLIDERS
	//[DllImport(ErosDll)] public static extern IntPtr	Collider_Box_Create		(string szName, uint nFlags, Vector3 vecPos, Quaternion quatRot, Vector3 vecBoxSize, float nDensityOrMass, int eColGroup);
	//[DllImport(ErosDll)] public static extern IntPtr	Collider_Sphere_Create	(string szName, uint nFlags, Vector3 vecPos, float nRadius, float nDensityOrMass, int eColGroup);
	//[DllImport(ErosDll)] public static extern IntPtr	Collider_Capsule_Create	(string szName, uint nFlags, Vector3 vecPos, Quaternion quatRot, float nRadius, float nHalfHeight, float nDensityOrMass, uint nCapAxisRotate, int eColGroup);
	//[DllImport(ErosDll)] public static extern IntPtr	Collider_Plane_Create	(string szName, uint nFlags, Vector3 vecPos, Quaternion quatRot, float nDensityOrMass, int eColGroup);

	//[DllImport(ErosDll)] public static extern void		Collider_Capsule_Update	(IntPtr pColPtr, float nRadius, float nHalfHeight);
	//[DllImport(ErosDll)] public static extern void		Collider_Box_Update(IntPtr pColPtr, Vector3 vecSize);
	
	//[DllImport(ErosDll)] public static extern void		Collider_Destroy(IntPtr pColPtr);
	//[DllImport(ErosDll)] public static extern void		Collider_EnableDisable(IntPtr pColPtr, bool bEnable);
	//[DllImport(ErosDll)] public static extern void		Collider_SetPositionRotation(IntPtr pColPtr, Vector3 vecPos, Quaternion quatRot);

	//[DllImport(ErosDll)] public static extern IntPtr	BodyCol_Init(int nVerts, IntPtr aVertsPtr, IntPtr aNormalsPtr, int nTris, IntPtr aTris, int nEdges, IntPtr aEdgesPtr, IntPtr aVertToVertsPtr);
	//[DllImport(ErosDll)] public static extern void		BodyCol_Destroy(IntPtr pBodyColPtr);
	//[DllImport(ErosDll)] public static extern void		BodyCol_Update(IntPtr pBodyColPtr, bool bFullRebuild, bool bPenisInVagina);
	//[DllImport(ErosDll)] public static extern int		BodyCol_RayCast(IntPtr pBodyColPtr, Vector3 vecOrigin, Vector3 vecUnitDir, IntPtr aVecRayHitInfoPtr);

	////---------------------------------------------------------------------------	BODY COLLIDER (FLUID AND CLOTH REPEL)
	//[DllImport(ErosDll)] public static extern IntPtr	BodyColCloth_Create(int nVerts, IntPtr aVertsPtr, IntPtr aNormalsPtr, int nTris, IntPtr aTris);
	//[DllImport(ErosDll)] public static extern void		BodyColCloth_Destroy(IntPtr pBodyColClothPtr);
	//[DllImport(ErosDll)] public static extern IntPtr	BodyColBreast_Create(int nBreastID, int nVerts, IntPtr aVertsPtr, IntPtr aNormalsPtr, int nVertSphereRadiusRatio, IntPtr aVertSphereRadiusRatioPtr, int nCapsuleSpheres, IntPtr aCapsuleSpheresPtr);
	//[DllImport(ErosDll)] public static extern void		BodyColBreast_Destroy(IntPtr pBodyColBreastPtr);

	//---------------------------------------------------------------------------	BLENDER INTERFACE
	//[DllImport(PhysX3)] public static extern bool		gBL_StartBlender(string sPathRuntime);
	[DllImport(ErosDll)] public static extern bool		gBL_HandshakeBlender();
	[DllImport(ErosDll)] public static extern bool		gBL_Init(string sPathRuntime);
	[DllImport(ErosDll)] public static extern void		gBL_Exit();
	[DllImport(ErosDll)] public static extern IntPtr	gBL_Cmd(string sCmd, bool bExpectRawBytes);
	[DllImport(ErosDll)] public static extern void 		gBL_Cmd_GetLastInBuf(IntPtr pBufUnityPtr, int nSize);

	//---------------------------------------------------------------------------	BLENDER MESH ACCESS
	[DllImport(ErosDll)] public static extern int		gBL_GetMeshArrays(string sNameMesh, int nMaterials, IntPtr pVertsPtr, IntPtr pNormalsPtr, IntPtr pUVsPtr);
	[DllImport(ErosDll)] public static extern int		gBL_UpdateClientVerts(string sNameMesh, IntPtr pVertsPtr);
	[DllImport(ErosDll)] public static extern int		gBL_UpdateBlenderVerts(string sNameMesh, IntPtr pVertsPtr);
	[DllImport(ErosDll)] public static extern int		gBL_GetNumTrianglesAtMaterial(int nMaterial);
	[DllImport(ErosDll)] public static extern void		gBL_GetTrianglesAtMaterial(int nMaterial, IntPtr aTris);

	////---------------------------------------------------------------------------	PENIS
	//[DllImport(ErosDll)] public static extern IntPtr	Penis_Create(string sNameObject, int nNumProps, int nBodyID, int nNumChainLinks, Vector3 vecPosRoot, Vector3 vecPosExtremity, Quaternion quatRotParent, float nRadiusInner, float nRadiusOuter, float nDensity);  //###IMPROVE: Rotation does nothing... have to rotate base later... ###DESIGN: Keep this way because of rotation dependencies of linked chain??
	//[DllImport(ErosDll)] public static extern void		Penis_Destroy(IntPtr pPenisPtr);
	//[DllImport(ErosDll)] public static extern void 		Penis_Update(IntPtr pPenisPtr, Vector3 vecPenisRootBonePos, Quaternion quatPenisRootBoneRot, IntPtr pTransformPtr, float nScale, bool bPenisInVagina);

	//---------------------------------------------------------------------------	MISC
	///[DllImport(ErosDll)] public static extern IntPtr	Dev_GetDevString(int nStringID);
	///[DllImport(ErosDll)] public static extern void		Utility_ForceQuit_HACK();
	[DllImport(ErosDll)] public static extern int		Utility_Test_Return123_HACK();

	////---------------------------------------------------------------------------	IN DEVELOPMENT
	//[DllImport(ErosDll)] public static extern IntPtr	PhysX2_Collider_CreateCapsule(Vector3 aVecPos, Quaternion aQuatRot, float nRadius, float nHeight);
	//[DllImport(ErosDll)] public static extern void		PhysX2_Collider_UpdateCapsule(IntPtr pActorPtr, Vector3 aVecPos, Quaternion aQuatRot, float nRadius, float nHeight, bool bUpdatePosRot);
	//[DllImport(ErosDll)] public static extern void		PhysX2_Collider_EnableDisable(IntPtr pActorPtr, bool bEnable);
	//[DllImport(ErosDll)] public static extern void		Morph_ApplyMorphOpToMesh(IntPtr aVertsBasePtr, IntPtr aVertsOutPtr, int nVerts, IntPtr aMorphDeltaVertsPtr, int nMorphDeltaVerts, float nMorphStrength);
}


	//[DllImport(ErosDll)] public static extern void		SoftBody_Breasts_UpdateCBodyColBreast_PhysX3(IntPtr pSoftBodyPtr, IntPtr pBodyColBreastPtr, float nRadiusSphereBase, float nOutsideProtusion, int eColGroup);
