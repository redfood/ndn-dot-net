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
namespace net.named_data.jndn.encrypt {
	
	using ILOG.J2CsMapping.Collections;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.spec;
	using net.named_data.jndn;
	using net.named_data.jndn.encoding.der;
	using net.named_data.jndn.encrypt.algo;
	using net.named_data.jndn.security;
	using net.named_data.jndn.security.certificate;
	using net.named_data.jndn.util;
	
	/// <summary>
	/// A GroupManager manages keys and schedules for group members in a particular
	/// namespace.
	/// </summary>
	///
	/// @note This class is an experimental feature. The API may change.
	public class GroupManager {
		/// <summary>
		/// Create a group manager with the given values. The group manager namespace
		/// is /{prefix}/read/{dataType} .
		/// </summary>
		///
		/// <param name="prefix">The prefix for the group manager namespace.</param>
		/// <param name="dataType">The data type for the group manager namespace.</param>
		/// <param name="database"></param>
		/// <param name="keySize">The group key will be an RSA key with keySize bits.</param>
		/// <param name="freshnessHours"></param>
		/// <param name="keyChain"></param>
		public GroupManager(Name prefix, Name dataType, GroupManagerDb database,
				int keySize, int freshnessHours, KeyChain keyChain) {
			namespace_ = new Name(prefix).append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_READ)
					.append(dataType);
			database_ = database;
			keySize_ = keySize;
			freshnessHours_ = freshnessHours;
	
			keyChain_ = keyChain;
		}
	
		/// <summary>
		/// Create a group key for the interval into which timeSlot falls. This creates
		/// a group key if it doesn't exist, and encrypts the key using the public key
		/// of each eligible member.
		/// </summary>
		///
		/// <param name="timeSlot">The time slot to cover as milliseconds since Jan 1, 1970 UTC.</param>
		/// <returns>A List of Data packets where the first is the E-KEY data packet
		/// with the group's public key and the rest are the D-KEY data packets with
		/// the group's private key encrypted with the public key of each eligible
		/// member. (Use List without generics so it works with older Java compilers.)</returns>
		/// <exception cref="GroupManagerDb.Error">for a database error.</exception>
		/// <exception cref="System.Security.SecurityException">for an error using the security KeyChain.</exception>
		public IList getGroupKey(double timeSlot) {
			IDictionary memberKeys = new SortedList();
			IList result = new ArrayList();
	
			// Get the time interval.
			Interval finalInterval = calculateInterval(timeSlot, memberKeys);
			if (finalInterval.isValid() == false)
				return result;
	
			String startTimeStamp = net.named_data.jndn.encrypt.Schedule.toIsoString(finalInterval
					.getStartTime());
			String endTimeStamp = net.named_data.jndn.encrypt.Schedule.toIsoString(finalInterval.getEndTime());
	
			// Generate the private and public keys.
			Blob[] privateKeyBlob = { null };
			Blob[] publicKeyBlob = { null };
			generateKeyPair(privateKeyBlob, publicKeyBlob);
	
			// Add the first element to the result.
			// The E-KEY (public key) data packet name convention is:
			// /<data_type>/E-KEY/[start-ts]/[end-ts]
			Data data = createEKeyData(startTimeStamp, endTimeStamp,
					publicKeyBlob[0]);
			ILOG.J2CsMapping.Collections.Collections.Add(result,data);
	
			// Encrypt the private key with the public key from each member's certificate.
			for (IIterator i = new ILOG.J2CsMapping.Collections.IteratorAdapter(memberKeys.GetEnumerator()); i.HasNext();) {
				DictionaryEntry entry = (DictionaryEntry) i.Next();
				Name keyName = (Name) ((DictionaryEntry) entry).Key;
				Blob certificateKey = (Blob) ((DictionaryEntry) entry).Value;
	
				// Generate the name of the packet.
				// The D-KEY (private key) data packet name convention is:
				// /<data_type>/D-KEY/[start-ts]/[end-ts]/[member-name]
				data = createDKeyData(startTimeStamp, endTimeStamp, keyName,
						privateKeyBlob[0], certificateKey);
				ILOG.J2CsMapping.Collections.Collections.Add(result,data);
			}
	
			return result;
		}
	
		/// <summary>
		/// Add a schedule with the given scheduleName.
		/// </summary>
		///
		/// <param name="scheduleName">The name of the schedule. The name cannot be empty.</param>
		/// <param name="schedule">The Schedule to add.</param>
		/// <exception cref="GroupManagerDb.Error">if a schedule with the same name already exists,if the name is empty, or other database error.</exception>
		public void addSchedule(String scheduleName, Schedule schedule) {
			database_.addSchedule(scheduleName, schedule);
		}
	
		/// <summary>
		/// Delete the schedule with the given scheduleName. Also delete members which
		/// use this schedule. If there is no schedule with the name, then do nothing.
		/// </summary>
		///
		/// <param name="scheduleName">The name of the schedule.</param>
		/// <exception cref="GroupManagerDb.Error">for a database error.</exception>
		public void deleteSchedule(String scheduleName) {
			database_.deleteSchedule(scheduleName);
		}
	
		/// <summary>
		/// Update the schedule with scheduleName and replace the old object with the
		/// given schedule. Otherwise, if no schedule with name exists, a new schedule
		/// with name and the given schedule will be added to database.
		/// </summary>
		///
		/// <param name="scheduleName">The name of the schedule. The name cannot be empty.</param>
		/// <param name="schedule">The Schedule to update or add.</param>
		/// <exception cref="GroupManagerDb.Error">if the name is empty, or other database error.</exception>
		public void updateSchedule(String scheduleName, Schedule schedule) {
			database_.updateSchedule(scheduleName, schedule);
		}
	
		/// <summary>
		/// Add a new member with the given memberCertificate into a schedule named
		/// scheduleName. If cert is an IdentityCertificate made from memberCertificate,
		/// then the member's identity name is cert.getPublicKeyName().getPrefix(-1).
		/// </summary>
		///
		/// <param name="scheduleName">The schedule name.</param>
		/// <param name="memberCertificate">The member's certificate.</param>
		/// <exception cref="GroupManagerDb.Error">If there's no schedule named scheduleName, ifthe member's identity name already exists, or other database error.</exception>
		/// <exception cref="DerDecodingException">for error decoding memberCertificate as acertificate.</exception>
		public void addMember(String scheduleName, Data memberCertificate) {
			IdentityCertificate cert = new IdentityCertificate(memberCertificate);
			database_.addMember(scheduleName, cert.getPublicKeyName(), cert
					.getPublicKeyInfo().getKeyDer());
		}
	
		/// <summary>
		/// Remove a member with the given identity name. If there is no member with
		/// the identity name, then do nothing.
		/// </summary>
		///
		/// <param name="identity">The member's identity name.</param>
		/// <exception cref="GroupManagerDb.Error">for a database error.</exception>
		public void removeMember(Name identity) {
			database_.deleteMember(identity);
		}
	
		/// <summary>
		/// Change the name of the schedule for the given member's identity name.
		/// </summary>
		///
		/// <param name="identity">The member's identity name.</param>
		/// <param name="scheduleName">The new schedule name.</param>
		/// <exception cref="GroupManagerDb.Error">if there's no member with the given identityname in the database, or there's no schedule named scheduleName.</exception>
		public void updateMemberSchedule(Name identity, String scheduleName) {
			database_.updateMemberSchedule(identity, scheduleName);
		}
	
		/// <summary>
		/// Calculate an Interval that covers the timeSlot.
		/// </summary>
		///
		/// <param name="timeSlot">The time slot to cover as milliseconds since Jan 1, 1970 UTC.</param>
		/// <param name="memberKeys">of the public key and the value is the Blob of the public key DER. (Use Map without generics so it works with older Java compilers.)</param>
		/// <returns>The Interval covering the time slot.</returns>
		/// <exception cref="GroupManagerDb.Error">for a database error.</exception>
		private Interval calculateInterval(double timeSlot, IDictionary memberKeys) {
			// Prepare.
			Interval positiveResult = new Interval();
			Interval negativeResult = new Interval();
			memberKeys.clear();
	
			// Get the all intervals from the schedules.
			IList scheduleNames = database_.listAllScheduleNames();
			for (int i = 0; i < scheduleNames.Count; ++i) {
				String scheduleName = (String) scheduleNames[i];
	
				Schedule schedule = database_.getSchedule(scheduleName);
				Schedule.Result result = schedule.getCoveringInterval(timeSlot);
				Interval tempInterval = result.interval;
	
				if (result.isPositive) {
					if (!positiveResult.isValid())
						positiveResult = tempInterval;
					positiveResult.intersectWith(tempInterval);
	
					IDictionary map = database_.getScheduleMembers(scheduleName);
					ILOG.J2CsMapping.Collections.Collections.PutAll(memberKeys,map);
				} else {
					if (!negativeResult.isValid())
						negativeResult = tempInterval;
					negativeResult.intersectWith(tempInterval);
				}
			}
			if (!positiveResult.isValid())
				// Return an invalid interval when there is no member which has an
				// interval covering the time slot.
				return new Interval(false);
	
			// Get the final interval result.
			Interval finalInterval;
			if (negativeResult.isValid())
				finalInterval = positiveResult.intersectWith(negativeResult);
			else
				finalInterval = positiveResult;
	
			return finalInterval;
		}
	
		/// <summary>
		/// Generate an RSA key pair according to keySize_.
		/// </summary>
		///
		/// <param name="privateKeyBlob"></param>
		/// <param name="publicKeyBlob"></param>
		private void generateKeyPair(Blob[] privateKeyBlob, Blob[] publicKeyBlob) {
			RsaKeyParams paras = new RsaKeyParams(keySize_);
	
			DecryptKey privateKey;
			try {
				privateKey = net.named_data.jndn.encrypt.algo.RsaAlgorithm.generateKey(paras);
			} catch (Exception ex) {
				// We don't expect this error.
				throw new Exception("Error in RsaAlgorithm.generateKey: "
						+ ex.Message);
			}
	
			privateKeyBlob[0] = privateKey.getKeyBits();
	
			EncryptKey publicKey;
			try {
				publicKey = net.named_data.jndn.encrypt.algo.RsaAlgorithm.deriveEncryptKey(privateKeyBlob[0]);
			} catch (InvalidKeySpecException ex_0) {
				// We don't expect this error.
				throw new Exception("Error in RsaAlgorithm.deriveEncryptKey: "
						+ ex_0.Message);
			} catch (DerDecodingException ex_1) {
				// We don't expect this error.
				throw new Exception("Error in RsaAlgorithm.deriveEncryptKey: "
						+ ex_1.Message);
			}
	
			publicKeyBlob[0] = publicKey.getKeyBits();
		}
	
		/// <summary>
		/// Create an E-KEY Data packet for the given public key.
		/// </summary>
		///
		/// <param name="startTimeStamp">The start time stamp string to put in the name.</param>
		/// <param name="endTimeStamp">The end time stamp string to put in the name.</param>
		/// <param name="publicKeyBlob">A Blob of the public key DER.</param>
		/// <returns>The Data packet.</returns>
		/// <exception cref="System.Security.SecurityException">for an error using the security KeyChain.</exception>
		private Data createEKeyData(String startTimeStamp, String endTimeStamp,
				Blob publicKeyBlob) {
			Name name = new Name(namespace_);
			name.append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_E_KEY).append(startTimeStamp)
					.append(endTimeStamp);
	
			Data data = new Data(name);
			data.getMetaInfo().setFreshnessPeriod(
					freshnessHours_ * MILLISECONDS_IN_HOUR);
			data.setContent(publicKeyBlob);
			keyChain_.sign(data);
			return data;
		}
	
		/// <summary>
		/// Create a D-KEY Data packet with an EncryptedContent for the given private
		/// key, encrypted with the certificate key.
		/// </summary>
		///
		/// <param name="startTimeStamp">The start time stamp string to put in the name.</param>
		/// <param name="endTimeStamp">The end time stamp string to put in the name.</param>
		/// <param name="keyName"></param>
		/// <param name="privateKeyBlob">A Blob of the encoded private key.</param>
		/// <param name="certificateKey"></param>
		/// <returns>The Data packet.</returns>
		/// <exception cref="System.Security.SecurityException">for an error using the security KeyChain.</exception>
		private Data createDKeyData(String startTimeStamp, String endTimeStamp,
				Name keyName, Blob privateKeyBlob, Blob certificateKey) {
			Name name = new Name(namespace_);
			name.append(net.named_data.jndn.encrypt.algo.Encryptor.NAME_COMPONENT_D_KEY);
			name.append(startTimeStamp).append(endTimeStamp);
			Data data = new Data(name);
			data.getMetaInfo().setFreshnessPeriod(
					freshnessHours_ * MILLISECONDS_IN_HOUR);
			EncryptParams encryptParams = new EncryptParams(
					net.named_data.jndn.encrypt.algo.EncryptAlgorithmType.RsaOaep);
			try {
				net.named_data.jndn.encrypt.algo.Encryptor.encryptData(data, privateKeyBlob, keyName,
						certificateKey, encryptParams);
			} catch (Exception ex) {
				// Consolidate errors such as InvalidKeyException.
				throw new SecurityException(
						"createDKeyData: Error in encryptData: " + ex.Message);
			}
	
			keyChain_.sign(data);
			return data;
		}
	
		/// <summary>
		/// A class implements Friend if it has a method setGroupManagerFriendAccess
		/// which setFriendAccess calls to set the FriendAccess object.
		/// </summary>
		///
		public interface Friend {
			void setGroupManagerFriendAccess(GroupManager.FriendAccess  friendAccess);
		}
	
		/// <summary>
		/// Call friend.setGroupManagerFriendAccess to pass an instance of
		/// a FriendAccess class to allow a friend class to call private methods.
		/// </summary>
		///
		/// <param name="friend">Therefore, only a friend class gets an implementation of FriendAccess.</param>
		public static void setFriendAccess(GroupManager.Friend  friend) {
			if (friend
							.GetType().FullName
					.equals("src.net.named_data.jndn.tests.integration_tests.TestGroupManager")) {
				friend.setGroupManagerFriendAccess(new GroupManager.FriendAccessImpl ());
			}
		}
	
		/// <summary>
		/// A friend class can call the methods of FriendAccess to access private
		/// methods.  This abstract class is public, but setFriendAccess passes an
		/// instance of a private class which implements the methods.
		/// </summary>
		///
		public abstract class FriendAccess {
			public abstract Interval calculateInterval(GroupManager groupManager,
					double timeSlot, IDictionary memberKeys);
	
			public abstract Data createDKeyData(GroupManager groupManager,
					String startTimeStamp, String endTimeStamp, Name keyName,
					Blob privateKeyBlob, Blob certificateKey);
	
			public abstract Data createEKeyData(GroupManager groupManager,
					String startTimeStamp, String endTimeStamp, Blob publicKeyBlob);
		}
	
		/// <summary>
		/// setFriendAccess passes an instance of this private class which implements
		/// the FriendAccess methods.
		/// </summary>
		///
		private class FriendAccessImpl : GroupManager.FriendAccess  {
			public override Interval calculateInterval(GroupManager groupManager,
					double timeSlot, IDictionary memberKeys) {
				return groupManager.calculateInterval(timeSlot, memberKeys);
			}
	
			public override Data createDKeyData(GroupManager groupManager,
					String startTimeStamp, String endTimeStamp, Name keyName,
					Blob privateKeyBlob, Blob certificateKey) {
				return groupManager.createDKeyData(startTimeStamp, endTimeStamp,
						keyName, privateKeyBlob, certificateKey);
			}
	
			public override Data createEKeyData(GroupManager groupManager,
					String startTimeStamp, String endTimeStamp, Blob publicKeyBlob) {
				return groupManager.createEKeyData(startTimeStamp, endTimeStamp,
						publicKeyBlob);
			}
		}
	
		private readonly Name namespace_;
		private readonly GroupManagerDb database_;
		private readonly int keySize_;
		private readonly int freshnessHours_;
		private readonly KeyChain keyChain_;
	
		private const long MILLISECONDS_IN_HOUR = 3600 * 1000;
	}
}
