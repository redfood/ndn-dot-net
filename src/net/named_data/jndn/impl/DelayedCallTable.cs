// --------------------------------------------------------------------------------------------------
// This file was automatically generated by J2CS Translator (http://j2cstranslator.sourceforge.net/). 
// Version 1.3.6.20110331_01     
//
// ${CustomMessageForDisclaimer}                                                                             
// --------------------------------------------------------------------------------------------------
 /// <summary>
/// Copyright (C) 2015-2016 Regents of the University of California.
/// </summary>
///
namespace net.named_data.jndn.impl {
	
	using ILOG.J2CsMapping.Util;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using net.named_data.jndn.util;
	
	/// <summary>
	/// DelayedCallTable is an internal class used by the Node implementation of
	/// callLater to store callbacks and call them when they time out.
	/// </summary>
	///
	public class DelayedCallTable {
		public DelayedCallTable() {
			this.table_ = new ArrayList<Entry>();
		}
	
		/// <summary>
		/// Call callback.run() after the given delay. This adds to the delayed call
		/// table which is used by callTimedOut().
		/// </summary>
		///
		/// <param name="delayMilliseconds">The delay in milliseconds.</param>
		/// <param name="callback">This calls callback.run() after the delay.</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void callLater(double delayMilliseconds,
				IRunnable callback) {
			DelayedCallTable.Entry  entry = new DelayedCallTable.Entry (delayMilliseconds, callback);
			// Insert into table_, sorted on getCallTime().
			// Search from the back since we expect it to go there.
			int i = table_.Count - 1;
			while (i >= 0) {
				if ((table_[i]).getCallTime() <= entry.getCallTime())
					break;
				--i;
			}
			// Element i is the greatest less than or equal to
			// entry.getCallTime(), so insert after it.
			table_.Insert(i + 1, entry);
		}
	
		/// <summary>
		/// Call and remove timed-out callback entries. Since callLater does a sorted
		/// insert into the delayed call table, the check for timed-out entries is
		/// quick and does not require searching the entire table. This synchronizes on
		/// the delayed call table when checking it, but not when calling the callback.
		/// </summary>
		///
		public void callTimedOut() {
			double now = net.named_data.jndn.util.Common.getNowMilliseconds();
			// table_ is sorted on _callTime, so we only need to process the timed-out
			// entries at the front, then quit.
			while (true) {
				DelayedCallTable.Entry  entry;
				 lock (this) {
								if ((table_.Count==0))
									break;
								entry = table_[0];
								if (entry.getCallTime() > now)
									// It is not time to call the entry at the front of the list, so finish.
									break;
								ILOG.J2CsMapping.Collections.Collections.RemoveAt(table_,0);
							}
	
				// The lock on table_ is removed, so call the callback.
				entry.callCallback();
			}
		}
	
		/// <summary>
		/// Entry holds the callback and other fields for an entry in the delayed call
		/// table.
		/// </summary>
		///
		private class Entry {
			/// <summary>
			/// Create a new DelayedCallTable.Entry and set the call time based on the
			/// current time and the delayMilliseconds.
			/// </summary>
			///
			/// <param name="delayMilliseconds">The delay in milliseconds.</param>
			/// <param name="callback">This calls callback.run() after the delay.</param>
			public Entry(double delayMilliseconds, IRunnable callback) {
				callback_ = callback;
				callTime_ = net.named_data.jndn.util.Common.getNowMilliseconds() + delayMilliseconds;
			}
	
			/// <summary>
			/// Get the time at which the callback should be called.
			/// </summary>
			///
			/// <returns>The call time in milliseconds, similar to
			/// Common.getNowMilliseconds().</returns>
			public double getCallTime() {
				return callTime_;
			}
	
			/// <summary>
			/// Call the callback given to the constructor. This does not catch
			/// exceptions.
			/// </summary>
			///
			public void callCallback() {
				callback_.run();
			}
	
			private readonly IRunnable callback_;
			private readonly double callTime_;
		}
	
		private readonly ArrayList<Entry> table_;
		// This is to force an import of net.named_data.jndn.util.
		private static Common dummyCommon_ = new Common();
	}
}
