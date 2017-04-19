/***TODO: COrificeRig:
- CGame.INSTANCE.gameObject???  Our own component?  On what node do we stuff our stuff??
- Vagina textures!
- For Anus to work we'll have to have the rings defined for it in addtition to the 'skin effect' move Vagina bones through Flex rig
- Why does rig creation crash Blender??
- Bad vert group corruption with pelvis... there still??
*/

using System.Collections.Generic;
using UnityEngine;

public class COrificeRig : uFlex.IFlexProcessor {
	CBody _oBody;
	Transform _oBoneRootT;
	const ushort C_nPinsTriangulation = 3;                      // Number of pins defined in Blender bone structure for vagina
	ushort _nStartOfTriangulationPinParticles;

	ushort _nRings;
	ushort _nVertsPerRing;
	Transform[] _aBonesRing0;
	Transform[] _aBonesPins;

    uFlex.FlexParticles         _oFlexParticles;
    uFlex.FlexShapeMatching     _oFlexShapeMatching;
    uFlex.FlexParticlesRenderer _oFlexParticlesRenderer;


	public COrificeRig(CBody oBody) {
		_oBody = oBody;

		//===== A. DE-SERIALIZE BLENDER DATA =====
		//=== Read the complex collection of 'CSoftBodySkin_VertRimInfo' objects Blender has prepared for us ===
		List<float> aRawData = CByteArray.GetArray_FLOAT("'CBody'", _oBody._oBodyBase._sBlenderInstancePath_CBodyBase + ".CreateOrificeRig()");
		List<Vector3> aVecs = new List<Vector3>();
		Vector3[,] aaVecs;

		//=== De-serialize the COrificeRig from Blender ===
		ushort nDataPointer = 0;
		_nRings					= (ushort)aRawData[nDataPointer++];
		_nVertsPerRing			= (ushort)aRawData[nDataPointer++];
		ushort nNumRigVerts		= (ushort)(_nRings * _nVertsPerRing);
		_nStartOfTriangulationPinParticles = nNumRigVerts;			// Tetrahedra pin particles are stored right after the simulated particles and there are always four of them
		ushort nShapes			= nNumRigVerts;						// Each rig vert has its shape (same index as the particle / vert)
		aaVecs = new Vector3[_nRings, _nVertsPerRing];

		//=== De-serialize each vert from each 'ring' to form our Flex collider rig ===
		for (ushort nRing = 0; nRing < _nRings; nRing++) {
			for (ushort nVert = 0; nVert < _nVertsPerRing; nVert++) {
				Vector3 vec = new Vector3();
				vec.x = -aRawData[nDataPointer++];			//###HACK: Direct rotation between RightHandRule and LeftHandRule between Blender and Unity (A vert of x=1, y=2, z=3 in Blender has that vert one meter to the left of the character, two meters behind and three meters above.  In Unity this needs to be x=-1, y=3, z=-2   so xU=-xB, yU=zB, zU=-yB)
				vec.z = -aRawData[nDataPointer++];
				vec.y =  aRawData[nDataPointer++];
				aaVecs[nRing, nVert] = vec;
			}
		}

		//===== B. CREATE PARTICLES =====
		//=== Create the particles that will be simulated (i.e. move) in response to Flex collision to drive the skinned presentation mesh via pretinent bones ===
		List<uFlex.Particle> aParticles = new List<uFlex.Particle>();
		for (ushort nRing = 0; nRing < _nRings; nRing++) {
			for (ushort nVert = 0; nVert < _nVertsPerRing; nVert++) {
				uFlex.Particle oParticle = new uFlex.Particle();
				oParticle.pos		= aaVecs[nRing, nVert];
				oParticle.invMass	= 1;
				aParticles.Add(oParticle);
			}
		}

		//===== C. OBTAIN REFERENCES TO PINS (IN) AND BONES (OUT) =====
		//=== Obtain access to the bones we'll manually move at each frame for ring0 Flex particle positions ===
		_oBoneRootT = _oBody._oBodyBase.FindBone("chestUpper/chestLower/abdomenUpper/abdomenLower/hip/pelvis/Genitals/Vagina");
		_aBonesRing0 = new Transform[_nVertsPerRing];
		for (ushort nBone = 0; nBone < _nVertsPerRing; nBone++) {
			string sNameBone = "VaginaBone" + nBone.ToString("D2");			//###LEARN: How to zero pad in .net
			_aBonesRing0[nBone] = _oBoneRootT.FindChild(sNameBone);
		}

		//=== Obtain reference to the 'triangulation bones' that are responsible to hold the entire soft body rig at the proper 3D space in relation to our owning body's reference bone (e.g. pelvis)
		_aBonesPins = new Transform[C_nPinsTriangulation];
		for (ushort nPin = 0; nPin < C_nPinsTriangulation; nPin++) {
			uFlex.Particle oParticle = new uFlex.Particle();
			string sNamePin = "VaginaPin" + nPin.ToString();
			Transform oPinT = _oBoneRootT.FindChild(sNamePin);
			oParticle.pos		= oPinT.position;
			oParticle.invMass	= 0;							// Particle is a pin and therere moved manually at every frame
			aParticles.Add(oParticle);							// The four pins start at 'nNumRigVerts' in aParticles[]
			_aBonesPins[nPin] = oPinT;
		}

		//===== D. CREATE THE SHAPES =====
		List<int> aShapeParticleIndices = new List<int>();
		List<int> aShapeParticleCutoffs = new List<int>();

		for (int nRing1 = 0; nRing1 < _nRings; nRing1++) {
			for (int nVert1 = 0; nVert1 < _nVertsPerRing; nVert1++) {
				int nParticle1 = nRing1 * _nVertsPerRing + nVert1;

				for (int nRing2 = 0; nRing2 < _nRings; nRing2++) {
					if (Mathf.Abs(nRing1 - nRing2) > 1)					// We only form shape links with rings immediately adjacent to ourselves ('nRing1')
						continue;

					for (int nVert2 = nVert1-1; nVert2 <= nVert1+1; nVert2++) {
						int nVert2Real = nVert2;
						if (nVert2Real < 0)
							nVert2Real = _nVertsPerRing - 1;
						if (nVert2Real >= _nVertsPerRing)
							nVert2Real = 0;

						int nParticle2 = nRing2 * _nVertsPerRing + nVert2Real;
						aShapeParticleIndices.Add(nParticle2);
						//Debug.LogFormat("-- #{0,3} - [{1,1}-{2,2}] - [{3,1}-{4,2}]", aShapeParticleIndices.Count, nRing1, nVert1, nRing2, nVert2Real);
					}
				}

				//--- Add the triangulation pins to each vert (so whole rig doesn't go floating in space) ---
				for (ushort nPin = 0; nPin < C_nPinsTriangulation; nPin++)
					aShapeParticleIndices.Add(_nStartOfTriangulationPinParticles + nPin);

				//--- Cutoff the particle index here ---
				//Debug.LogFormat("-- #{0,3} - [{1,1}-{2,2}] CUT at {3,2}", aShapeParticleIndices.Count, nRing1, nVert1, aShapeParticleIndices.Count);
				aShapeParticleCutoffs.Add(aShapeParticleIndices.Count);						// Specify the 'cutoff' for the next shape to be where we're at in our index collection once we're done with this shape
			}
		}



		//===== E. CREATE FLEX PARTICLES OBJECT =====
		//=== Define Flex particles from Blender mesh made for Flex ===
		int nParticles = aParticles.Count;
		_oFlexParticles = CUtility.FindOrCreateComponent(CGame.INSTANCE.gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
		_oFlexParticles.m_particlesCount = _oFlexParticles.m_maxParticlesCount = nParticles;
		_oFlexParticles.m_particles = aParticles.ToArray();
        _oFlexParticles.m_restParticles = aParticles.ToArray();		//###OPT19: Wasteful?  Remove from uFlex??
		_oFlexParticles.m_colours = new Color[nParticles];
		_oFlexParticles.m_velocities = new Vector3[nParticles];
		_oFlexParticles.m_densities = new float[nParticles];
		_oFlexParticles.m_particlesActivity = new bool[nParticles];
		_oFlexParticles.m_colour = Color.green;                //###TODO: Colors!
		_oFlexParticles.m_interactionType = uFlex.FlexInteractionType.SelfCollideFiltered;
		_oFlexParticles.m_collisionGroup = -1;
        _oFlexParticles.m_bounds.SetMinMax(new Vector3(-1,-1,-1), new Vector3(1,1,1));        //###CHECK: Better with some reasonable values than zero?
		for (int nParticle = 0; nParticle < nParticles; nParticle++) {
			_oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
			_oFlexParticles.m_particlesActivity[nParticle] = true;
		}


		//===== F. CREATE FLEX SHAPES =====
		//=== Define Flex shapes from the Blender particles that have been set as shapes too ===
		_oFlexShapeMatching = CUtility.FindOrCreateComponent(CGame.INSTANCE.gameObject, typeof(uFlex.FlexShapeMatching)) as uFlex.FlexShapeMatching;
		_oFlexShapeMatching.m_shapesCount = nShapes;
		_oFlexShapeMatching.m_shapeIndicesCount = aShapeParticleIndices.Count;
		_oFlexShapeMatching.m_shapeIndices = aShapeParticleIndices.ToArray();            //###LEARN: How to convert a list to a straight .Net array.
		_oFlexShapeMatching.m_shapeOffsets = aShapeParticleCutoffs.ToArray();
		_oFlexShapeMatching.m_shapeCenters = new Vector3[nShapes];
		_oFlexShapeMatching.m_shapeCoefficients = new float[nShapes];
		_oFlexShapeMatching.m_shapeTranslations = new Vector3[nShapes];
		_oFlexShapeMatching.m_shapeRotations = new Quaternion[nShapes];
		_oFlexShapeMatching.m_shapeRestPositions = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];

		//=== Calculate shape centers from attached particles ===
		int nShapeStart = 0;			//###LEARN: Setting shape center to our vert center makes the result much less stable!  (By definition I think resting state assumes shape is truly at center of its related particles!)
		for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
			_oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.1f;                   //###NOW###   Calculate shape center here or just accept particle pos?

			int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
			Vector3 vecCenter = new Vector3();
			for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
				int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
				Vector3 vecParticlePos = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
				vecCenter += vecParticlePos;
			}

			vecCenter /= (nShapeEnd - nShapeStart);
			_oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
			nShapeStart = nShapeEnd;
		}

		//===== Set the shape rest positions =====
		nShapeStart = 0;
		int nShapeIndexOffset = 0;
		for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
			int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
			for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
				int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
				Vector3 vecParticle = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
				_oFlexShapeMatching.m_shapeRestPositions[nShapeIndexOffset] = vecParticle - _oFlexShapeMatching.m_shapeCenters[nShape];
				nShapeIndexOffset++;
			}
			nShapeStart = nShapeEnd;
		}


		//===== G. ADD RENDERERS =====
		//=== Add particle renderer ===
		_oFlexParticlesRenderer = CUtility.FindOrCreateComponent(CGame.INSTANCE.gameObject, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
		_oFlexParticlesRenderer.m_size = CGame.INSTANCE.particleSpacing;
		_oFlexParticlesRenderer.m_radius = _oFlexParticlesRenderer.m_size / 2.0f;
		_oFlexParticlesRenderer.enabled = false;           // Hidden by default

        //=== Instantiate the debug visualizer for internal Softbody structure analysis ===
        CVisualizeSoftBody oVisSB = CUtility.FindOrCreateComponent(CGame.INSTANCE.gameObject, typeof(CVisualizeSoftBody)) as CVisualizeSoftBody;
        oVisSB.enabled = true;

        //=== Instantiate the FlexProcessor component so we get hooks to update ourselves during game frames ===
        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(CGame.INSTANCE.gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
        oFlexProc._oFlexProcessor = this;
	}


	public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		//=== Set the pinned particles to the position of the bone pins.  This prevents the entire rig from floating in space ===
		for (ushort nPin = 0; nPin < C_nPinsTriangulation; nPin++)
			_oFlexParticles.m_particles[_nStartOfTriangulationPinParticles + nPin].pos = _aBonesPins[nPin].position;
		//=== Set the position of the skinned bones to the ring 0 particle positions.  This morphs the user-visible mesh ===
		for (ushort nBone = 0; nBone < _nVertsPerRing; nBone++)
			_aBonesRing0[nBone].position = _oFlexParticles.m_particles[nBone].pos;
	}
}
