// --------------------------------------------------------------------------------------------------
// This file was automatically generated by J2CS Translator (http://j2cstranslator.sourceforge.net/). 
// Version 1.3.6.20110331_01     
// 12/23/15 3:55 PM    
// ${CustomMessageForDisclaimer}                                                                             
// --------------------------------------------------------------------------------------------------
 /// <summary>
/// Copyright (C) 2013-2015 Regents of the University of California.
/// </summary>
///
namespace net.named_data.jndn {
	
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	
	/// <summary>
	/// A class implements OnData if it has onData, used to pass a callback to
	/// Face.expressInterest.
	/// </summary>
	///
	public interface OnData {
		/// <summary>
		/// When a matching data packet is received, onData is called.
		/// </summary>
		///
		/// <param name="interest"></param>
		/// <param name="data">The received Data object.</param>
		void onData(Interest interest, Data data);
	}
}