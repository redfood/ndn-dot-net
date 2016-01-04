// --------------------------------------------------------------------------------------------------
// This file was automatically generated by J2CS Translator (http://j2cstranslator.sourceforge.net/). 
// Version 1.3.6.20110331_01     
// 12/23/15 3:55 PM    
// ${CustomMessageForDisclaimer}                                                                             
// --------------------------------------------------------------------------------------------------
 /// <summary>
/// Copyright (C) 2015 Regents of the University of California.
/// </summary>
///
namespace net.named_data.jndn.encrypt {
	
	using ILOG.J2CsMapping.Collections;
	using ILOG.J2CsMapping.Util.Logging;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.spec;
	using javax.crypto;
	using net.named_data.jndn;
	using net.named_data.jndn.encoding;
	using net.named_data.jndn.encrypt.algo;
	using net.named_data.jndn.security;
	using net.named_data.jndn.util;
	
	/// <summary>
	/// A Producer manages content keys used to encrypt a data packet in the
	/// group-based encryption protocol.
	/// </summary>
	///
	/// @note This class is an experimental feature. The API may change.
	public class Producer {
		public sealed class Anonymous_C1 : OnData {
				private readonly Producer outer_Producer;
				private readonly Producer.KeyRequest  keyRequest;
				private readonly Producer.OnEncryptedKeys  onEncryptedKeys;
				private readonly double timeSlot;
		
				public Anonymous_C1(Producer paramouter_Producer,
						Producer.KeyRequest  keyRequest_0, Producer.OnEncryptedKeys  onEncryptedKeys_1,
						double timeSlot_2) {
					this.keyRequest = keyRequest_0;
					this.onEncryptedKeys = onEncryptedKeys_1;
					this.timeSlot = timeSlot_2;
					this.outer_Producer = paramouter_Producer;
				}
		
				public void onData(Interest interest, Data data) {
					try {
						outer_Producer.handleCoveringKey(interest, data, timeSlot, keyRequest,
								onEncryptedKeys);
					} catch (Exception ex) {
						ILOG.J2CsMapping.Util.Logging.Logger.getLogger(typeof(Producer).FullName).log(
								ILOG.J2CsMapping.Util.Logging.Level.SEVERE, null, ex);
					}
				}
			}
	
		public sealed class Anonymous_C0 : OnTimeout {
				private readonly Producer outer_Producer;
				private readonly Producer.KeyRequest  keyRequest;
				private readonly Producer.OnEncryptedKeys  onEncryptedKeys;
				private readonly double timeSlot;
		
				public Anonymous_C0(Producer paramouter_Producer,
						Producer.KeyRequest  keyRequest_0, Producer.OnEncryptedKeys  onEncryptedKeys_1,
						double timeSlot_2) {
					this.keyRequest = keyRequest_0;
					this.onEncryptedKeys = onEncryptedKeys_1;
					this.timeSlot = timeSlot_2;
					this.outer_Producer = paramouter_Producer;
				}
		
				public void onTimeout(Interest interest) {
					try {
						outer_Producer.handleTimeout(interest, timeSlot, keyRequest,
								onEncryptedKeys);
					} catch (IOException ex) {
						ILOG.J2CsMapping.Util.Logging.Logger.getLogger(typeof(Producer).FullName).log(
								ILOG.J2CsMapping.Util.Logging.Level.SEVERE, null, ex);
					}
				}
			}
	
		public interface OnEncryptedKeys {
			// List is a list of Data packets with the content key encrypted by E-KEYS.
			void onEncryptedKeys(IList keys);
		}
	
		/// <summary>
		/// Create a Producer to use the given ProducerDb, Face and other values.
		/// A producer can produce data with a naming convention:
		/// /&lt;prefix>/SAMPLE/&lt;dataType>/[timestamp]
		/// The produced data packet is encrypted with a content key,
		/// which is stored in the ProducerDb database.
		/// A producer also needs to produce data containing a content key
		/// encrypted with E-KEYs. A producer can retrieve E-KEYs through the face,
		/// and will re-try for at most repeatAttemps times when E-KEY retrieval fails.
		/// </summary>
		///
		/// <param name="prefix">The producer name prefix. This makes a copy of the Name.</param>
		/// <param name="dataType"></param>
		/// <param name="face">The face used to retrieve keys.</param>
		/// <param name="keyChain">The keyChain used to sign data packets.</param>
		/// <param name="database">The ProducerDb database for storing keys.</param>
		/// <param name="repeatAttempts">The maximum retry for retrieving keys.</param>
		public Producer(Name prefix, Name dataType, Face face, KeyChain keyChain,
				ProducerDb database, int repeatAttempts) {
			this.eKeyInfo_ = new Hashtable();
					this.keyRequests_ = new Hashtable();
			face_ = face;
			keyChain_ = keyChain;
			database_ = database;
			maxRepeatAttempts_ = repeatAttempts;
	
			construct(prefix, dataType);
		}
	
		/// <summary>
		/// Create a Producer to use the given ProducerDb, Face and other values.
		/// A producer can produce data with a naming convention:
		/// /&lt;prefix>/SAMPLE/&lt;dataType>/[timestamp]
		/// The produced data packet is encrypted with a content key,
		/// which is stored in the ProducerDb database.
		/// A producer also needs to produce data containing a content key
		/// encrypted with E-KEYs. A producer can retrieve E-KEYs through the face,
		/// and will re-try for at most 3 times when E-KEY retrieval fails.
		/// </summary>
		///
		/// <param name="prefix">The producer name prefix.</param>
		/// <param name="dataType">The dataType portion of the producer name.</param>
		/// <param name="face">The face used to retrieve keys.</param>
		/// <param name="keyChain">The keyChain used to sign data packets.</param>
		/// <param name="database">The ProducerDb database for storing keys.</param>
		public Producer(Name prefix, Name dataType, Face face, KeyChain keyChain,
				ProducerDb database) {
			this.eKeyInfo_ = new Hashtable();
					this.keyRequests_ = new Hashtable();
			face_ = face;
			keyChain_ = keyChain;
			database_ = database;
			maxRepeatAttempts_ = 3;
	
			construct(prefix, dataType);
		}
	
		private void construct(Name prefix, Name dataType) {
			Name fixedPrefix = new Name(prefix);
			Name fixedDataType = new Name(dataType);
	
			// Fill ekeyInfo_ with all permutations of dataType, including the 'E-KEY'
			// component of the name. This will be used in createContentKey to send
			// interests without reconstructing names every time.
			fixedPrefix.append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_READ);
			while (fixedDataType.size() > 0) {
				Name nodeName = new Name(fixedPrefix);
				nodeName.append(fixedDataType);
				nodeName.append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_E_KEY);
	
				ILOG.J2CsMapping.Collections.Collections.Put(eKeyInfo_,nodeName,new Producer.KeyInfo ());
				fixedDataType = fixedDataType.getPrefix(-1);
			}
			fixedPrefix.append(dataType);
			namespace_ = new Name(prefix);
			namespace_.append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_SAMPLE);
			namespace_.append(dataType);
		}
	
		/// <summary>
		/// Create the content key. This first checks if the content key exists. For an
		/// existing content key, this returns the content key name directly. If the
		/// key does not exist, this creates one and encrypts it using the
		/// corresponding E-KEYs. The encrypted content keys are passed to the
		/// onEncryptedKeys callback.
		/// </summary>
		///
		/// <param name="timeSlot_0">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <param name="onEncryptedKeys_1">content key Data packets. If onEncryptedKeys is null, this does not use it.</param>
		/// <returns>The content key name.</returns>
		public Name createContentKey(double timeSlot_0,
				Producer.OnEncryptedKeys  onEncryptedKeys_1) {
			double hourSlot = getRoundedTimeSlot(timeSlot_0);
	
			// Create the content key name.
			Name contentKeyName = new Name(namespace_);
			contentKeyName.append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_C_KEY);
			contentKeyName.append(net.named_data.jndn.encrypt.Schedule.toIsoString(hourSlot));
	
			Blob contentKeyBits;
			if (database_.hasContentKey(timeSlot_0)) {
				contentKeyBits = database_.getContentKey(timeSlot_0);
				return contentKeyName;
			}
	
			AesKeyParams aesParams = new AesKeyParams(128);
			contentKeyBits = net.named_data.jndn.encrypt.algo.AesAlgorithm.generateKey(aesParams).getKeyBits();
			database_.addContentKey(timeSlot_0, contentKeyBits);
	
			double timeCount = timeSlot_0;
			ILOG.J2CsMapping.Collections.Collections.Put(keyRequests_,timeCount,new Producer.KeyRequest (eKeyInfo_.Count));
			Producer.KeyRequest  keyRequest_2 = (Producer.KeyRequest ) ILOG.J2CsMapping.Collections.Collections.Get(keyRequests_,timeCount);
	
			Exclude timeRange = new Exclude();
			excludeAfter(timeRange,
					new Name.Component(net.named_data.jndn.encrypt.Schedule.toIsoString(timeSlot_0)));
			// Send interests for all nodes in the tree.
			new ILOG.J2CsMapping.Collections.IteratorAdapter(eKeyInfo_.GetEnumerator());
			for (IIterator i = new ILOG.J2CsMapping.Collections.IteratorAdapter(eKeyInfo_.GetEnumerator()); i.HasNext();) {
				DictionaryEntry entry = (DictionaryEntry) i.Next();
				Producer.KeyInfo  keyInfo = (Producer.KeyInfo ) ((DictionaryEntry) entry).Value;
				ILOG.J2CsMapping.Collections.Collections.Put(keyRequest_2.repeatAttempts,((DictionaryEntry) entry).Key,0);
				if (timeSlot_0 < keyInfo.beginTimeSlot
						|| timeSlot_0 >= keyInfo.endTimeSlot) {
					sendKeyInterest((Name) ((DictionaryEntry) entry).Key, timeSlot_0, keyRequest_2,
							onEncryptedKeys_1, timeRange);
				} else {
					Name eKeyName = new Name((Name) ((DictionaryEntry) entry).Key);
					eKeyName.append(net.named_data.jndn.encrypt.Schedule.toIsoString(keyInfo.beginTimeSlot));
					eKeyName.append(net.named_data.jndn.encrypt.Schedule.toIsoString(keyInfo.endTimeSlot));
					encryptContentKey(keyRequest_2, keyInfo.keyBits, eKeyName,
							timeSlot_0, onEncryptedKeys_1);
				}
			}
	
			return contentKeyName;
		}
	
		/// <summary>
		/// Encrypt the given content with the content key that covers timeSlot, and
		/// update the data packet with the encrypted content and an appropriate data
		/// name.
		/// </summary>
		///
		/// <param name="data">An empty Data object which is updated.</param>
		/// <param name="timeSlot_0">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <param name="content">The content to encrypt.</param>
		public void produce(Data data, double timeSlot_0, Blob content) {
			Name contentKeyName = new Name(createContentKey(timeSlot_0, null));
			Blob contentKey = database_.getContentKey(timeSlot_0);
	
			Name dataName = new Name(namespace_);
			dataName.append(net.named_data.jndn.encrypt.Schedule.toIsoString(getRoundedTimeSlot(timeSlot_0)));
	
			data.setName(dataName);
			EncryptParams paras = new EncryptParams(net.named_data.jndn.encrypt.algo.EncryptAlgorithmType.AesCbc,
					16);
			net.named_data.jndn.encrypt.algo.Encryptor
					.encryptData(data, content, contentKeyName, contentKey, paras);
			keyChain_.sign(data);
		}
	
		private class KeyInfo {
			public double beginTimeSlot;
			public double endTimeSlot;
			public Blob keyBits;
		}
	
		internal class KeyRequest {
			public KeyRequest(int interests) {
				this.repeatAttempts = new Hashtable();
				this.encryptedKeys = new ArrayList();
				interestCount = interests;
			}
	
			public int interestCount;
			public readonly IDictionary repeatAttempts;
			/// <summary>
			/// < The map key is the Name. The value is an int count. 
			/// </summary>
			///
			public readonly IList encryptedKeys; // of Data.
		}
	
		/// <summary>
		/// Round timeSlot to the nearest whole hour, so that we can store content keys
		/// uniformly (by start of the hour).
		/// </summary>
		///
		/// <param name="timeSlot_0">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <returns>The start of the hour as milliseconds since Jan 1, 1970 UTC.</returns>
		private static double getRoundedTimeSlot(double timeSlot_0) {
			return Math.Round(Math.Floor(Math.Round(timeSlot_0,MidpointRounding.AwayFromZero) / 3600000.0d) * 3600000.0d,MidpointRounding.AwayFromZero);
		}
	
		/// <summary>
		/// Send an interest with the given name through the face with callbacks to
		/// handleCoveringKey and handleTimeout.
		/// </summary>
		///
		/// <param name="name">The name of the interest to send.</param>
		/// <param name="timeSlot_0"></param>
		/// <param name="keyRequest_1"></param>
		/// <param name="onEncryptedKeys_2"></param>
		/// <param name="timeRange">The Exclude for the interest.</param>
		private void sendKeyInterest(Name name, double timeSlot_0,
				Producer.KeyRequest  keyRequest_1, Producer.OnEncryptedKeys  onEncryptedKeys_2,
				Exclude timeRange) {
			OnData onKey = new Producer.Anonymous_C1 (this, keyRequest_1, onEncryptedKeys_2, timeSlot_0);
	
			OnTimeout onTimeout = new Producer.Anonymous_C0 (this, keyRequest_1, onEncryptedKeys_2, timeSlot_0);
	
			Interest keyInterest = new Interest(name);
			keyInterest.setExclude(timeRange);
			keyInterest.setChildSelector(1);
	
			face_.expressInterest(keyInterest, onKey, onTimeout);
		}
	
		/// <summary>
		/// This is called from an expressInterest timeout to update the state of
		/// keyRequest.
		/// </summary>
		///
		/// <param name="interest">The timed-out interest.</param>
		/// <param name="timeSlot_0">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <param name="keyRequest_1">The KeyRequest which is updated.</param>
		/// <param name="onEncryptedKeys_2">encrypted content key Data packets. If onEncryptedKeys is null, this does not use it.</param>
		internal void handleTimeout(Interest interest, double timeSlot_0,
				Producer.KeyRequest  keyRequest_1, Producer.OnEncryptedKeys  onEncryptedKeys_2) {
			Name interestName = interest.getName();
	
			if ((int) (Int32) ILOG.J2CsMapping.Collections.Collections.Get(keyRequest_1.repeatAttempts,interestName) < maxRepeatAttempts_) {
				ILOG.J2CsMapping.Collections.Collections.Put(keyRequest_1.repeatAttempts,interestName,(int) (Int32) ILOG.J2CsMapping.Collections.Collections.Get(keyRequest_1.repeatAttempts,interestName) + 1);
				sendKeyInterest(interestName, timeSlot_0, keyRequest_1,
						onEncryptedKeys_2, interest.getExclude());
			} else
				--keyRequest_1.interestCount;
	
			if (keyRequest_1.interestCount == 0 && onEncryptedKeys_2 != null) {
				onEncryptedKeys_2.onEncryptedKeys(keyRequest_1.encryptedKeys);
				ILOG.J2CsMapping.Collections.Collections.Remove(keyRequests_,timeSlot_0);
			}
		}
	
		/// <summary>
		/// This is called from an expressInterest OnData to check that the encryption
		/// key contained in data fits the timeSlot. This sends a refined interest if
		/// required.
		/// </summary>
		///
		/// <param name="interest">The interest given to expressInterest.</param>
		/// <param name="data">The fetched Data packet.</param>
		/// <param name="timeSlot_0">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <param name="keyRequest_1">The KeyRequest which is updated.</param>
		/// <param name="onEncryptedKeys_2">encrypted content key Data packets. If onEncryptedKeys is null, this does not use it.</param>
		internal void handleCoveringKey(Interest interest, Data data,
				double timeSlot_0, Producer.KeyRequest  keyRequest_1,
				Producer.OnEncryptedKeys  onEncryptedKeys_2) {
			Name interestName = interest.getName();
			Name keyName = data.getName();
	
			double begin = net.named_data.jndn.encrypt.Schedule.fromIsoString(keyName.get(iStartTimeStamp)
					.getValue().toString());
			double end = net.named_data.jndn.encrypt.Schedule.fromIsoString(keyName.get(iEndTimeStamp)
					.getValue().toString());
	
			if (timeSlot_0 >= end) {
				Exclude timeRange = new Exclude(interest.getExclude());
				excludeBefore(timeRange, keyName.get(iStartTimeStamp));
				ILOG.J2CsMapping.Collections.Collections.Put(keyRequest_1.repeatAttempts,interestName,0);
				sendKeyInterest(interestName, timeSlot_0, keyRequest_1,
						onEncryptedKeys_2, timeRange);
				return;
			}
	
			Blob encryptionKey = data.getContent();
			Producer.KeyInfo  keyInfo = (Producer.KeyInfo ) ILOG.J2CsMapping.Collections.Collections.Get(eKeyInfo_,interestName);
			keyInfo.beginTimeSlot = begin;
			keyInfo.endTimeSlot = end;
			keyInfo.keyBits = encryptionKey;
	
			encryptContentKey(keyRequest_1, encryptionKey, keyName, timeSlot_0,
					onEncryptedKeys_2);
		}
	
		/// <summary>
		/// Get the content key from the database_ and encrypt it for the timeSlot
		/// using encryptionKey.
		/// </summary>
		///
		/// <param name="keyRequest_0">The KeyRequest which is updated.</param>
		/// <param name="encryptionKey">The encryption key value.</param>
		/// <param name="eKeyName">The key name for the EncryptedContent.</param>
		/// <param name="timeSlot_1">The time slot as milliseconds since Jan 1, 1970 UTC.</param>
		/// <param name="onEncryptedKeys_2">encrypted content key Data packets. If onEncryptedKeys is null, this does not use it.</param>
		private void encryptContentKey(Producer.KeyRequest  keyRequest_0, Blob encryptionKey,
				Name eKeyName, double timeSlot_1, Producer.OnEncryptedKeys  onEncryptedKeys_2) {
			Name keyName = new Name(namespace_);
			keyName.append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_C_KEY);
			keyName.append(net.named_data.jndn.encrypt.Schedule.toIsoString(getRoundedTimeSlot(timeSlot_1)));
	
			Blob contentKey = database_.getContentKey(timeSlot_1);
	
			Data cKeyData = new Data();
			cKeyData.setName(keyName);
			EncryptParams paras = new EncryptParams(net.named_data.jndn.encrypt.algo.EncryptAlgorithmType.RsaOaep);
			try {
				net.named_data.jndn.encrypt.algo.Encryptor.encryptData(cKeyData, contentKey, eKeyName,
						encryptionKey, paras);
			} catch (Exception ex) {
				// Consolidate errors such as InvalidKeyException.
				throw new SecurityException(
						"encryptContentKey: Error in encryptData: "
								+ ex.Message);
			}
	
			keyChain_.sign(cKeyData);
			ILOG.J2CsMapping.Collections.Collections.Add(keyRequest_0.encryptedKeys,cKeyData);
	
			--keyRequest_0.interestCount;
			if (keyRequest_0.interestCount == 0 && onEncryptedKeys_2 != null) {
				onEncryptedKeys_2.onEncryptedKeys(keyRequest_0.encryptedKeys);
				ILOG.J2CsMapping.Collections.Collections.Remove(keyRequests_,timeSlot_1);
			}
		}
	
		// TODO: Move this to be the main representation inside the Exclude object.
		private class ExcludeEntry {
			public ExcludeEntry(Name.Component component,
					bool anyFollowsComponent) {
				component_ = component;
				anyFollowsComponent_ = anyFollowsComponent;
			}
	
			public Name.Component component_;
			public bool anyFollowsComponent_;
		}
	
		/// <summary>
		/// Create a list of ExcludeEntry from the Exclude object.
		/// </summary>
		///
		/// <param name="exclude">The Exclude object to read.</param>
		/// <returns>A new list of ExcludeEntry.</returns>
		private static ArrayList getExcludeEntries(Exclude exclude) {
			ArrayList entries = new ArrayList();
	
			for (int i = 0; i < exclude.size(); ++i) {
				if (exclude.get(i).getType() == net.named_data.jndn.Exclude.Type.ANY) {
					if (entries.Count == 0)
						// Add a "beginning ANY".
						ILOG.J2CsMapping.Collections.Collections.Add(entries,new Producer.ExcludeEntry (new Name.Component(), true));
					else
						// Set anyFollowsComponent of the final component.
						((Producer.ExcludeEntry ) entries[entries.Count - 1]).anyFollowsComponent_ = true;
				} else
					ILOG.J2CsMapping.Collections.Collections.Add(entries,new Producer.ExcludeEntry (exclude.get(i).getComponent(),
											false));
			}
	
			return entries;
		}
	
		/// <summary>
		/// Set the Exclude object from the list of ExcludeEntry.
		/// </summary>
		///
		/// <param name="exclude">The Exclude object to update.</param>
		/// <param name="entries">The list of ExcludeEntry.</param>
		private static void setExcludeEntries(Exclude exclude, ArrayList entries) {
			exclude.clear();
	
			for (int i = 0; i < entries.Count; ++i) {
				Producer.ExcludeEntry  entry = (Producer.ExcludeEntry ) entries[i];
	
				if (i == 0 && entry.component_.getValue().size() == 0
						&& entry.anyFollowsComponent_)
					// This is a "beginning ANY".
					exclude.appendAny();
				else {
					exclude.appendComponent(entry.component_);
					if (entry.anyFollowsComponent_)
						exclude.appendAny();
				}
			}
		}
	
		/// <summary>
		/// Get the latest entry in the list whose component_ is less than or equal to
		/// component.
		/// </summary>
		///
		/// <param name="entries">The list of ExcludeEntry.</param>
		/// <param name="component">The component to compare.</param>
		/// <returns>The index of the found entry, or -1 if not found.</returns>
		private static int findEntryBeforeOrAt(ArrayList entries,
				Name.Component component) {
			int i = entries.Count - 1;
			while (i >= 0) {
				if (((Producer.ExcludeEntry ) entries[i]).component_.compare(component) <= 0)
					break;
				--i;
			}
	
			return i;
		}
	
		/// <summary>
		/// Exclude all components in the range beginning at "from".
		/// </summary>
		///
		/// <param name="exclude">The Exclude object to update.</param>
		/// <param name="from">The first component in the exclude range.</param>
		private static void excludeAfter(Exclude exclude, Name.Component from) {
			ArrayList entries = getExcludeEntries(exclude);
	
			int iNewFrom;
			int iFoundFrom = findEntryBeforeOrAt(entries, from);
			if (iFoundFrom < 0) {
				// There is no entry before "from" so insert at the beginning.
				entries.Insert(0, new Producer.ExcludeEntry (from, true));
				iNewFrom = 0;
			} else {
				Producer.ExcludeEntry  foundFrom = (Producer.ExcludeEntry ) entries[iFoundFrom];
	
				if (!foundFrom.anyFollowsComponent_) {
					if (foundFrom.component_.equals(from)) {
						// There is already an entry with "from", so just set the "ANY" flag.
						foundFrom.anyFollowsComponent_ = true;
						iNewFrom = iFoundFrom;
					} else {
						// Insert following the entry before "from".
						entries.Insert(iFoundFrom + 1, new Producer.ExcludeEntry (from, true));
						iNewFrom = iFoundFrom + 1;
					}
				} else
					// The entry before "from" already has an "ANY" flag, so do nothing.
					iNewFrom = iFoundFrom;
			}
	
			// Remove entries after the new "from".
			int iRemoveBegin = iNewFrom + 1;
			int nRemoveNeeded = entries.Count - iRemoveBegin;
			for (int i = 0; i < nRemoveNeeded; ++i)
				ILOG.J2CsMapping.Collections.Collections.RemoveAt(entries,iRemoveBegin);
	
			setExcludeEntries(exclude, entries);
		}
	
		/// <summary>
		/// Exclude all components in the range ending at "to".
		/// </summary>
		///
		/// <param name="exclude">The Exclude object to update.</param>
		/// <param name="to">The last component in the exclude range.</param>
		private static void excludeBefore(Exclude exclude, Name.Component to) {
			excludeRange(exclude, new Name.Component(), to);
		}
	
		/// <summary>
		/// Exclude all components in the range beginning at "from" and ending at "to".
		/// </summary>
		///
		/// <param name="exclude">The Exclude object to update.</param>
		/// <param name="from">The first component in the exclude range.</param>
		/// <param name="to">The last component in the exclude range.</param>
		private static void excludeRange(Exclude exclude, Name.Component from,
				Name.Component to) {
			if (from.compare(to) >= 0) {
				if (from.compare(to) == 0)
					throw new Exception(
							"excludeRange: from == to. To exclude a single component, sue excludeOne.");
				else
					throw new Exception(
							"excludeRange: from must be less than to. Invalid range: ["
									+ from.toEscapedString() + ", "
									+ to.toEscapedString() + "]");
			}
	
			ArrayList entries = getExcludeEntries(exclude);
	
			int iNewFrom;
			int iFoundFrom = findEntryBeforeOrAt(entries, from);
			if (iFoundFrom < 0) {
				// There is no entry before "from" so insert at the beginning.
				entries.Insert(0, new Producer.ExcludeEntry (from, true));
				iNewFrom = 0;
			} else {
				Producer.ExcludeEntry  foundFrom = (Producer.ExcludeEntry ) entries[iFoundFrom];
	
				if (!foundFrom.anyFollowsComponent_) {
					if (foundFrom.component_.equals(from)) {
						// There is already an entry with "from", so just set the "ANY" flag.
						foundFrom.anyFollowsComponent_ = true;
						iNewFrom = iFoundFrom;
					} else {
						// Insert following the entry before "from".
						entries.Insert(iFoundFrom + 1, new Producer.ExcludeEntry (from, true));
						iNewFrom = iFoundFrom + 1;
					}
				} else
					// The entry before "from" already has an "ANY" flag, so do nothing.
					iNewFrom = iFoundFrom;
			}
	
			// We have at least one "from" before "to", so we know this will find an entry.
			int iFoundTo = findEntryBeforeOrAt(entries, to);
			Producer.ExcludeEntry  foundTo = (Producer.ExcludeEntry ) entries[iFoundTo];
			if (iFoundTo == iNewFrom)
				// Insert the "to" immediately after the "from".
				entries.Insert(iNewFrom + 1, new Producer.ExcludeEntry (to, false));
			else {
				int iRemoveEnd;
				if (!foundTo.anyFollowsComponent_) {
					if (foundTo.component_.equals(to))
						// The "to" entry already exists. Remove up to it.
						iRemoveEnd = iFoundTo;
					else {
						// Insert following the previous entry, which will be removed.
						entries.Insert(iFoundTo + 1, new Producer.ExcludeEntry (to, false));
						iRemoveEnd = iFoundTo + 1;
					}
				} else
					// "to" follows a component which is already followed by "ANY", meaning
					// the new range now encompasses it, so remove the component.
					iRemoveEnd = iFoundTo + 1;
	
				// Remove intermediate entries since they are inside the range.
				int iRemoveBegin = iNewFrom + 1;
				int nRemoveNeeded = iRemoveEnd - iRemoveBegin;
				for (int i = 0; i < nRemoveNeeded; ++i)
					ILOG.J2CsMapping.Collections.Collections.RemoveAt(entries,iRemoveBegin);
			}
	
			setExcludeEntries(exclude, entries);
		}
	
		private readonly Face face_;
		private Name namespace_;
		private readonly KeyChain keyChain_;
		// Use HashMap without generics so it works with older Java compilers.
		private readonly IDictionary eKeyInfo_;
		/// <summary>
		/// < The map key is the key Name. The value is a KeyInfo. 
		/// </summary>
		///
		private readonly IDictionary keyRequests_;
		/// <summary>
		/// < The map key is the double time stamp. The value is a KeyRequest. 
		/// </summary>
		///
		private readonly ProducerDb database_;
		private readonly int maxRepeatAttempts_;
	
		private const int iStartTimeStamp = -2;
		private const int iEndTimeStamp = -1;
	}
}
