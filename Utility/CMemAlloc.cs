using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class CMemAlloc<T> {									// Important helper class used throughout to expose Unity buffer to our C++ dll for ultra-fast two-way zero-copy data sharing
	public T[]				L;								// The local array of TYPE that Unity uses for local access
	public GCHandle			H;								// The GCHandle used to pin in memory.
	public IntPtr			P;								// The remote equivalent pointer we send Phys dll so both dll and Unity can read/write the same buffer.
	
	public CMemAlloc() {}									// Zero-argument constructor is for local arrays that are not shared with dll.
	public CMemAlloc(int nSizeArray) {
		Allocate(nSizeArray);
	}

	public T[] Allocate(int nSizeArray) {				//###IMPROVE: Redo as templates??
		L = new T[nSizeArray];
		PinInMemory();
		return L;
	}		
	public void AssignAndPin(T[] a) {
		L = a;
		PinInMemory();
	}	
	
	//public T[] AllocateFromArrayCopy(T[] aArray) {
	//	L = (T[])aArray.Clone();
	//	PinInMemory();
	//	return L;
	//}
	//public T[] AllocateFromArrayList(ArrayList aList) {
	//	L = (T[])aList.ToArray(typeof(T));
	//	PinInMemory();
	//	return L;
	//}
	//public T[] AllocateFromList(List<T> aList) {
	//	L = aList.ToArray();
	//	PinInMemory();
	//	return L;
	//}
	public void PinInMemory() {
		H = GCHandle.Alloc(L, GCHandleType.Pinned);
		P = H.AddrOfPinnedObject();
	}

	public void ReleaseGlobalHandles() {			//###CHECK!!!!!: Make sure this is called when destroying!!!
		P = IntPtr.Zero;			//###CHECK: Doing this correctly??
		H.Free();
	}
}

public enum EPhysDest {				// Who this message is for...		//###OBS??
	Manager,
	SoftBody,
	Cloth
};

public enum EPhysReq {				// Message request types sent by Phys in OnPhys_
	Message,
	Dump,
	MemAlloc,
	MemFree
};

public enum EPhysType {				// Message request data types sent by Phys in OnPhys_
	None,
	Particleices,
	TetraIndices
};
