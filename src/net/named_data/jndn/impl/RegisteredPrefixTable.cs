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
	
	using ILOG.J2CsMapping.Util.Logging;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using net.named_data.jndn;
	
	/// <summary>
	/// A RegisteredPrefixTable is an internal class to hold a list of registered
	/// prefixes with information necessary to remove the registration later.
	/// </summary>
	///
	public class RegisteredPrefixTable {
		/// <summary>
		/// Create a new RegisteredPrefixTable with an empty table.
		/// </summary>
		///
		/// <param name="interestFilterTable"></param>
		public RegisteredPrefixTable(InterestFilterTable interestFilterTable) {
			this.table_ = new ArrayList();
			interestFilterTable_ = interestFilterTable;
		}
	
		/// <summary>
		/// Add a new entry to the table.
		/// </summary>
		///
		/// <param name="registeredPrefixId">The ID from Node.getNextEntryId().</param>
		/// <param name="prefix">The name prefix.</param>
		/// <param name="relatedInterestFilterId">to 0.</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void add(long registeredPrefixId, Name prefix,
				long relatedInterestFilterId) {
			ILOG.J2CsMapping.Collections.Collections.Add(table_,new RegisteredPrefixTable.Entry (registeredPrefixId, prefix,
							relatedInterestFilterId));
		}
	
		/// <summary>
		/// Remove the registered prefix entry with the registeredPrefixId from the
		/// registered prefix table. This does not affect another registered prefix with
		/// a different registeredPrefixId, even if it has the same prefix name. If an
		/// interest filter was automatically created by registerPrefix, also call
		/// interestFilterTable_.unsetInterestFilter to remove it.
		/// If there is no entry with the registeredPrefixId, do nothing.
		/// </summary>
		///
		/// <param name="registeredPrefixId">The ID returned from registerPrefix.</param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void removeRegisteredPrefix(
				long registeredPrefixId) {
			int count = 0;
			// Go backwards through the list so we can remove entries.
			// Remove all entries even though registeredPrefixId should be unique.
			for (int i = table_.Count - 1; i >= 0; --i) {
				RegisteredPrefixTable.Entry  entry = (RegisteredPrefixTable.Entry ) table_[i];
	
				if (entry.getRegisteredPrefixId() == registeredPrefixId) {
					++count;
	
					if (entry.getRelatedInterestFilterId() > 0)
						// Remove the related interest filter.
						interestFilterTable_.unsetInterestFilter(entry
								.getRelatedInterestFilterId());
	
					ILOG.J2CsMapping.Collections.Collections.RemoveAt(table_,i);
				}
			}
	
			if (count == 0)
				logger_.log(
						ILOG.J2CsMapping.Util.Logging.Level.WARNING,
						"removeRegisteredPrefix: Didn't find registeredPrefixId {0}",
						registeredPrefixId);
		}
	
		/// <summary>
		/// A RegisteredPrefixTable.Entry holds a registeredPrefixId and information
		/// necessary to remove the registration later. It optionally holds a related
		/// interestFilterId if the InterestFilter was set in the same registerPrefix
		/// operation.
		/// </summary>
		///
		private class Entry {
			/// <summary>
			/// Create a RegisteredPrefixTable.Entry with the given values.
			/// </summary>
			///
			/// <param name="registeredPrefixId">The ID from Node.getNextEntryId().</param>
			/// <param name="prefix">The name prefix.</param>
			/// <param name="relatedInterestFilterId">to 0.</param>
			public Entry(long registeredPrefixId, Name prefix,
					long relatedInterestFilterId) {
				registeredPrefixId_ = registeredPrefixId;
				prefix_ = prefix;
				relatedInterestFilterId_ = relatedInterestFilterId;
			}
	
			/// <summary>
			/// Get the registeredPrefixId given to the constructor.
			/// </summary>
			///
			/// <returns>The registeredPrefixId.</returns>
			public long getRegisteredPrefixId() {
				return registeredPrefixId_;
			}
	
			/// <summary>
			/// Get the name prefix given to the constructor.
			/// </summary>
			///
			/// <returns>The name prefix.</returns>
			public Name getPrefix() {
				return prefix_;
			}
	
			/// <summary>
			/// Get the related interestFilterId given to the constructor.
			/// </summary>
			///
			/// <returns>The related interestFilterId.</returns>
			public long getRelatedInterestFilterId() {
				return relatedInterestFilterId_;
			}
	
			private readonly long registeredPrefixId_;
			/// <summary>
			/// < A unique identifier for this entry so it can be deleted 
			/// </summary>
			///
			private readonly Name prefix_;
			private readonly long relatedInterestFilterId_;
		}
	
		// Use ArrayList without generics so it works with older Java compilers.
		private readonly IList table_; // Entry
		private readonly InterestFilterTable interestFilterTable_;
		private static readonly Logger logger_ = ILOG.J2CsMapping.Util.Logging.Logger
				.getLogger(typeof(RegisteredPrefixTable).FullName);
	}
}
