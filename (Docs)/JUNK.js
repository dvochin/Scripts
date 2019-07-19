

//try {
//} catch (e) {
//   DUMP(e);
//}

//function ScriptLoad_LoadScript(sURL, fnCallback = null) {				//###IMPROVE: Harmony jscript supports 'import' instead.  Use that??
//	console.info("LoadScript() loading " + sURL);
//    var head = document.getElementsByTagName('head')[0];
//    var script = document.createElement('script');
//    script.type = 'text/javascript';
//    script.src = sURL + "?" + Math.floor(Math.random() * 10000);				//###DEV26: Keep randomizer to ensure no-cache??
//	script.onload = OnCodeLoaded;
//	// if (fnCallback !== null) {
//		// script.onreadystatechange = callback;
//		// script.onload = fnCallback;
//	// }
//    head.appendChild(script);
//}

////isc.showConsoleInline();




//isc.ListGrid.create({
//    ID: "eGridDump",
//    canResizeFields: 1,
//    height: "100%", width: "100%",
//    fields: [
//        { name: "D", title: "Dump Log" }
//    ]
//});
//isc.Portlet.create({
//    ID: "ePortDump",
//    title: "Dump Log",
//    height: '20%',
//    width: '100%'
//});
//ePortDump.addMember(eGridDump);


////--- OTHER ---
////isc.ListGrid.create({
////	ID: "eGridOther",
////	canResizeFields: 1,
////	height: "100%", width: "100%",
////	fields: [
////        { name: "D", title: "OTHER" }
////	]
////});
////isc.Portlet.create({
////	ID: "ePortOther",
////	title: "Other",
////	width: '100%'
////});
////ePortOther.addMember(eGridOther);


////--- PORTAL LAYOUT ---
//isc.PortalLayout.create({
//    ID: "ePortLayout",
//    top: 29,
//    width: '100%',
//    height: '100%',
//    numColumns: 1,
//    canResizeRows: 1,
//    showColumnMenus: 0
//});



//	//recordClick: function (viewer, record, recordNum, field, fieldNum, value, rawValue) { },
//	//recordDoubleClick: function (viewer, record, recordNum, field, fieldNum, value, rawValue) { },
//	// doubleClick: function() {			//###WEAK: Create new Body by double clicking outside of any record!
//		// if (eWndBody.isDrawn() == false || eWndBody.isVisible() == false) {
//			// DUMP("Starting new Body creation.");
//			// eWndBody.show();
//			// eFormBody.editNewRecord();
//		// }
//	// }

	
	
	
//------------ Old implementation with per-record added buttons -> grouped at the bottom now
//isc.ListGrid.create({
//    ID: "eGridBody",
//	dataSource:"Body",
//    autoFetchData: true,
//	canEdit: false,
//    canResizeFields: true,
//	canRemoveRecords: true,
//    showRecordComponents: true,				//###SOURCE: Custom-icon technique from 'gridCellWidgets' sample
//    showRecordComponentsByCell: true,
//    recordComponentPoolingMode: "recycle",

//	fields: [
//        { name: "Name" },
//        { name: "Descr" },
//        { name: "Sex" },
//        { name: "Part" },
//        { name: "Data" },
//        { name: "ICONS", title: " ", width:36 }
//	],
	
//    createRecordComponent : function (record, colNum) {  
//        var sNameField = this.getFieldName(colNum);  
//        if (sNameField == "ICONS") {  
//			return isc.HLayout.create({
//                height: 22,
//				members: [
//					isc.ImgButton.create({
//						prompt: "Edit Body Morphs.",
//						src: "edit.png",
//						showDown: false,
//						showRollOver: false,
//						height: 16, width: 16,
//						click : function () { Body_EditRecordInModalWindow(record); }
//					}),
//					isc.ImgButton.create({
//						prompt: "Apply these body morphs to the current body.",
//						src: "view_rtl.png",		//###IMPROVE: Right icons
//						showDown: false, 
//						showRollOver: false,
//						height: 16, width: 16,
//						click : function () { Body_LoadMorphPropertiesFromBodyRecord(record.Data); }
//					})
//				]
//            });
//        } else {  
//            return null;  
//        }  
//    },
	
//    updateRecordComponent: function (record, colNum, component, recordChanged) {
//        var sNameField = this.getFieldName(colNum);  	//###INFO: We must re-connect custom components callback functions to point the the right record.  (This is needed because custom components get 'recycled' when shown / hidden.  See 'grid cell widgets' sample)
//        if (sNameField == "ICONS") {
//            var aButtons = component.getMembers();
//			aButtons[0].addProperties({			//###INFO: Necessary duplication of the 'click' callback setting as done in 'createRecordComponent' above.  See 'grid cell widgets' sample for explanation why
//				click : function () { Body_EditRecordInModalWindow(record); }
//			});		
//			aButtons[1].addProperties({			//###INFO: Button 0 = edit, button 1 = apply as in createRecordComponent above
//				click : function () { Body_LoadMorphPropertiesFromBodyRecord(record.Data); }
//			});		
//        } else {  
//            return null;  
//        }  
//        return component;
//    }
//});








	
	
//// var Body_LoadMorphPropertiesFromBodyRecord = function(sProps_JSON) {
//// 	DUMP("#Body_LoadMorphPropertiesFromBodyRecord() loads from JSON: " + sProps_JSON);
//// 	var sProps_JSON = TreeProps_Load(sProps_JSON);			//###INFO: Save the non-zero properties current shown in the morphing properties into one flat JSON string that is stored in the database
//// }
////
//// var Body_SaveMorphPropertiesIntoBodyRecord = function() {
//// 	var sProps_JSON = TreeProps_Save();			//###INFO: Save the non-zero properties current shown in the morphing properties into one flat JSON string that is stored in the database
//// 	DUMP("#Body_SaveMorphPropertiesIntoBodyRecord() saves to JSON: " + sProps_JSON);
//// 	eFormBody.getField("Data").setValue(sProps_JSON);
//// 	eFormBody.saveData();
//// 	eWndBody.hide();
//// }


//	_CreateWndPopup: function(oRec) {
//		let eWndPopup = isc.CWndPopup.create({
//			ID: "CWndPopup_Body",
//			title: "Body Definition Fields",
//			_eEditor: this,

//			items: [
//				isc.DynamicForm.create({
//					dataSource: "Body",
//					cellPadding: 3,
//					margin: 5,

//					initWidget: function() {
//						let nFields = this.fields.length;
//						for (let i=0; i<nFields; i++) {
//							let oField = this.fields[i];
//							oField['width'] = 150;
//						}
//						return this.Super("initWidget", arguments);     //###INFO: How to run custom init code just before actual creation.
//					},
//					fields: [
//						{name: "Name", 			width: 100 },
//						{name: "Sex", 			width: 100 },
//						{name: "Part",			width: 100 },
//						{name: "Descr",			width: 100, height:40 },
//						{name: "Data", 			width: 100, title: "Binary Data" },
//						{type: "button", 		width: 100, title: "Save",  	endRow: false, 		click: function() { this.form.topElement._OnBtnSave(); }},      //###INFO: form points to owning 'DynamicForm' and its topElement points to the popup modal window we need here
//						{type: "button", 		width: 100, title: "Cancel", 	startRow: false, 	click: function() { this.form.topElement._OnBtnCancel(); }}
//					]
//				})
//			]
//		});
//	},
