// --------------------------------------------------------------------------------------------------
// This file was automatically generated by J2CS Translator (http://j2cstranslator.sourceforge.net/). 
// Version 1.3.6.20110331_01     
//
// ${CustomMessageForDisclaimer}                                                                             
// --------------------------------------------------------------------------------------------------
 /// <summary>
/// Copyright (C) 2014-2016 Regents of the University of California.
/// </summary>
///
namespace net.named_data.jndn {
	
	using ILOG.J2CsMapping.Collections;
	using ILOG.J2CsMapping.NIO;
	using ILOG.J2CsMapping.Util;
	using ILOG.J2CsMapping.Util.Logging;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using net.named_data.jndn.encoding;
	using net.named_data.jndn.encoding.tlv;
	using net.named_data.jndn.impl;
	using net.named_data.jndn.security;
	using net.named_data.jndn.transport;
	using net.named_data.jndn.util;
	
	/// <summary>
	/// The Node class implements internal functionality for the Face class.
	/// </summary>
	///
	public class Node : ElementListener {
		/// <summary>
		/// Create a new Node for communication with an NDN hub with the given
		/// Transport object and connectionInfo.
		/// </summary>
		///
		/// <param name="transport">A Transport object used for communication.</param>
		/// <param name="connectionInfo"></param>
		public Node(Transport transport, Transport.ConnectionInfo connectionInfo) {
			this.pendingInterestTable_ = new PendingInterestTable();
			this.interestFilterTable_ = new InterestFilterTable();
			this.registeredPrefixTable_ = new RegisteredPrefixTable(
					interestFilterTable_);
			this.delayedCallTable_ = new DelayedCallTable();
			this.onConnectedCallbacks_ = ILOG.J2CsMapping.Collections.Collections
					.synchronizedList(new ArrayList());
			this.commandInterestGenerator_ = new CommandInterestGenerator();
			this.timeoutPrefix_ = new Name("/local/timeout");
			this.lastEntryIdLock_ = new Object();
			this.connectStatus_ = net.named_data.jndn.Node.ConnectStatus.UNCONNECTED;
			transport_ = transport;
			connectionInfo_ = connectionInfo;
		}
	
		/// <summary>
		/// Send the Interest through the transport, read the entire response and call
		/// onData(interest, data).
		/// </summary>
		///
		/// <param name="pendingInterestId"></param>
		/// <param name="interest">The Interest to send.  This copies the Interest.</param>
		/// <param name="onData"></param>
		/// <param name="onTimeout"></param>
		/// <param name="wireFormat">A WireFormat object used to encode the message.</param>
		/// <param name="face"></param>
		/// <exception cref="IOException">For I/O error in sending the interest.</exception>
		/// <exception cref="System.Exception">If the encoded interest size exceeds getMaxNdnPacketSize().</exception>
		public void expressInterest(long pendingInterestId,
				Interest interest, OnData onData, OnTimeout onTimeout,
				WireFormat wireFormat, Face face) {
			Interest interestCopy = new Interest(interest);
	
			if (connectStatus_ == net.named_data.jndn.Node.ConnectStatus.CONNECT_COMPLETE) {
				// We are connected. Simply send the interest without synchronizing.
				expressInterestHelper(pendingInterestId, interestCopy, onData,
						onTimeout, wireFormat, face);
				return;
			}
	
			 lock (onConnectedCallbacks_) {
						// TODO: Properly check if we are already connected to the expected host.
						if (!transport_.isAsync()) {
							// The simple case: Just do a blocking connect and express.
							transport_.connect(connectionInfo_, this, null);
							expressInterestHelper(pendingInterestId, interestCopy, onData,
									onTimeout, wireFormat, face);
							// Make future calls to expressInterest send directly to the Transport.
							connectStatus_ = net.named_data.jndn.Node.ConnectStatus.CONNECT_COMPLETE;
			
							return;
						}
			
						// Handle the async case.
						if (connectStatus_ == net.named_data.jndn.Node.ConnectStatus.UNCONNECTED) {
							connectStatus_ = net.named_data.jndn.Node.ConnectStatus.CONNECT_REQUESTED;
			
							// expressInterestHelper will be called by onConnected.
							ILOG.J2CsMapping.Collections.Collections.Add(onConnectedCallbacks_,new Node.Anonymous_C3 (this, onData, pendingInterestId, face,
													wireFormat, interestCopy, onTimeout));
			
							IRunnable onConnected = new Node.Anonymous_C2 (this);
							transport_.connect(connectionInfo_, this, onConnected);
						} else if (connectStatus_ == net.named_data.jndn.Node.ConnectStatus.CONNECT_REQUESTED) {
							// Still connecting. add to the interests to express by onConnected.
							ILOG.J2CsMapping.Collections.Collections.Add(onConnectedCallbacks_,new Node.Anonymous_C1 (this, pendingInterestId, interestCopy, face,
													onTimeout, wireFormat, onData));
						} else if (connectStatus_ == net.named_data.jndn.Node.ConnectStatus.CONNECT_COMPLETE)
							// We have to repeat this check for CONNECT_COMPLETE in case the
							// onConnected callback was called while we were waiting to enter this
							// synchronized block.
							expressInterestHelper(pendingInterestId, interestCopy, onData,
									onTimeout, wireFormat, face);
						else
							// Don't expect this to happen.
							throw new Exception("Node: Unrecognized _connectStatus "
									+ connectStatus_);
					}
		}
	
		/// <summary>
		/// Remove the pending interest entry with the pendingInterestId from the
		/// pending interest table. This does not affect another pending interest with
		/// a different pendingInterestId, even if it has the same interest name.
		/// If there is no entry with the pendingInterestId, do nothing.
		/// </summary>
		///
		/// <param name="pendingInterestId">The ID returned from expressInterest.</param>
		public void removePendingInterest(long pendingInterestId) {
			pendingInterestTable_.removePendingInterest(pendingInterestId);
		}
	
		/// <summary>
		/// Append a timestamp component and a random value component to interest's
		/// name. Then use the keyChain and certificateName to sign the interest. If
		/// the interest lifetime is not set, this sets it.
		/// </summary>
		///
		/// <param name="interest">The interest whose name is append with components.</param>
		/// <param name="keyChain">The KeyChain object for signing interests.</param>
		/// <param name="certificateName">The certificate name for signing interests.</param>
		/// <param name="wireFormat"></param>
		/// <exception cref="System.Security.SecurityException">If cannot find the private key for thecertificateName.</exception>
		internal void makeCommandInterest(Interest interest, KeyChain keyChain,
				Name certificateName, WireFormat wireFormat) {
			commandInterestGenerator_.generate(interest, keyChain, certificateName,
					wireFormat);
		}
	
		/// <summary>
		/// Register prefix with the connected NDN hub and call onInterest when a
		/// matching interest is received. To register a prefix with NFD, you must
		/// first call setCommandSigningInfo.
		/// </summary>
		///
		/// <param name="registeredPrefixId"></param>
		/// <param name="prefix">A Name for the prefix to register. This copies the Name.</param>
		/// <param name="onInterest">onInterest.onInterest(prefix, interest, face, interestFilterId, filter). If onInterest is null, it is ignored and you must call setInterestFilter.</param>
		/// <param name="onRegisterFailed">prefix.</param>
		/// <param name="onRegisterSuccess">receives a success message from the forwarder. If onRegisterSuccess is null, this does not use it.</param>
		/// <param name="flags"></param>
		/// <param name="wireFormat">A WireFormat object used to encode the message.</param>
		/// <param name="commandKeyChain">The KeyChain object for signing interests.</param>
		/// <param name="commandCertificateName">The certificate name for signing interests.</param>
		/// <param name="face"></param>
		/// <exception cref="IOException">For I/O error in sending the registration request.</exception>
		/// <exception cref="System.Security.SecurityException">If signing a command interest for NFD and cannotfind the private key for the certificateName.</exception>
		public void registerPrefix(long registeredPrefixId, Name prefix,
				OnInterestCallback onInterest, OnRegisterFailed onRegisterFailed,
				OnRegisterSuccess onRegisterSuccess, ForwardingFlags flags,
				WireFormat wireFormat, KeyChain commandKeyChain,
				Name commandCertificateName, Face face) {
			nfdRegisterPrefix(registeredPrefixId, new Name(prefix), onInterest,
					onRegisterFailed, onRegisterSuccess, flags, commandKeyChain,
					commandCertificateName, wireFormat, face);
		}
	
		/// <summary>
		/// Remove the registered prefix entry with the registeredPrefixId from the
		/// registered prefix table. This does not affect another registered prefix with
		/// a different registeredPrefixId, even if it has the same prefix name. If an
		/// interest filter was automatically created by registerPrefix, also remove it.
		/// If there is no entry with the registeredPrefixId, do nothing.
		/// </summary>
		///
		/// <param name="registeredPrefixId">The ID returned from registerPrefix.</param>
		public void removeRegisteredPrefix(long registeredPrefixId) {
			registeredPrefixTable_.removeRegisteredPrefix(registeredPrefixId);
		}
	
		/// <summary>
		/// Add an entry to the local interest filter table to call the onInterest
		/// callback for a matching incoming Interest. This method only modifies the
		/// library's local callback table and does not register the prefix with the
		/// forwarder. It will always succeed. To register a prefix with the forwarder,
		/// use registerPrefix.
		/// </summary>
		///
		/// <param name="interestFilterId"></param>
		/// <param name="filter"></param>
		/// <param name="onInterest">onInterest.onInterest(prefix, interest, face, interestFilterId, filter).</param>
		/// <param name="face">The face which is passed to the onInterest callback.</param>
		public void setInterestFilter(long interestFilterId,
				InterestFilter filter, OnInterestCallback onInterest, Face face) {
			interestFilterTable_.setInterestFilter(interestFilterId,
					new InterestFilter(filter), onInterest, face);
		}
	
		/// <summary>
		/// Remove the interest filter entry which has the interestFilterId from the
		/// interest filter table. This does not affect another interest filter with
		/// a different interestFilterId, even if it has the same prefix name.
		/// If there is no entry with the interestFilterId, do nothing.
		/// </summary>
		///
		/// <param name="interestFilterId">The ID returned from setInterestFilter.</param>
		public void unsetInterestFilter(long interestFilterId) {
			interestFilterTable_.unsetInterestFilter(interestFilterId);
		}
	
		/// <summary>
		/// The OnInterestCallback calls this to put a Data packet which
		/// satisfies an Interest.
		/// </summary>
		///
		/// <param name="data">The Data packet which satisfies the interest.</param>
		/// <param name="wireFormat">A WireFormat object used to encode the Data packet.</param>
		/// <exception cref="System.Exception">If the encoded Data packet size exceeds getMaxNdnPacketSize().</exception>
		public void putData(Data data, WireFormat wireFormat) {
			Blob encoding = data.wireEncode(wireFormat);
			if (encoding.size() > getMaxNdnPacketSize())
				throw new Exception(
						"The encoded Data packet size exceeds the maximum limit getMaxNdnPacketSize()");
	
			transport_.send(encoding.buf());
		}
	
		/// <summary>
		/// Send the encoded packet out through the transport.
		/// </summary>
		///
		/// <param name="encoding"></param>
		/// <exception cref="System.Exception">If the encoded packet size exceeds getMaxNdnPacketSize().</exception>
		public void send(ByteBuffer encoding) {
			if (encoding.remaining() > getMaxNdnPacketSize())
				throw new Exception(
						"The encoded packet size exceeds the maximum limit getMaxNdnPacketSize()");
	
			transport_.send(encoding);
		}
	
		/// <summary>
		/// Process any packets to receive and call callbacks such as onData,
		/// onInterest or onTimeout. This returns immediately if there is no data to
		/// receive. This blocks while calling the callbacks. You should repeatedly
		/// call this from an event loop, with calls to sleep as needed so that the
		/// loop doesn't use 100% of the CPU. Since processEvents modifies the pending
		/// interest table, your application should make sure that it calls
		/// processEvents in the same thread as expressInterest (which also modifies
		/// the pending interest table).
		/// This may throw an exception for reading data or in the callback for
		/// processing the data. If you call this from an main event loop, you may want
		/// to catch and log/disregard all exceptions.
		/// </summary>
		///
		public void processEvents() {
			transport_.processEvents();
	
			// If Face.callLater is overridden to use a different mechanism, then
			// processEvents is not needed to check for delayed calls.
			delayedCallTable_.callTimedOut();
		}
	
		public Transport getTransport() {
			return transport_;
		}
	
		public Transport.ConnectionInfo getConnectionInfo() {
			return connectionInfo_;
		}
	
		public void onReceivedElement(ByteBuffer element) {
			LocalControlHeader localControlHeader = null;
			if (element.get(0) == net.named_data.jndn.encoding.tlv.Tlv.LocalControlHeader_LocalControlHeader) {
				// Decode the LocalControlHeader and replace element with the payload.
				localControlHeader = new LocalControlHeader();
				localControlHeader.wireDecode(element, net.named_data.jndn.encoding.TlvWireFormat.get());
				element = localControlHeader.getPayloadWireEncoding().buf();
			}
	
			// First, decode as Interest or Data.
			Interest interest = null;
			Data data = null;
			if (element.get(0) == net.named_data.jndn.encoding.tlv.Tlv.Interest || element.get(0) == net.named_data.jndn.encoding.tlv.Tlv.Data) {
				TlvDecoder decoder = new TlvDecoder(element);
				if (decoder.peekType(net.named_data.jndn.encoding.tlv.Tlv.Interest, element.remaining())) {
					interest = new Interest();
					interest.wireDecode(element, net.named_data.jndn.encoding.TlvWireFormat.get());
	
					if (localControlHeader != null)
						interest.setLocalControlHeader(localControlHeader);
				} else if (decoder.peekType(net.named_data.jndn.encoding.tlv.Tlv.Data, element.remaining())) {
					data = new Data();
					data.wireDecode(element, net.named_data.jndn.encoding.TlvWireFormat.get());
	
					if (localControlHeader != null)
						data.setLocalControlHeader(localControlHeader);
				}
			}
	
			// Now process as Interest or Data.
			if (interest != null) {
				// Quickly lock and get all interest filter callbacks which match.
				ArrayList matchedFilters = new ArrayList();
				interestFilterTable_.getMatchedFilters(interest, matchedFilters);
	
				// The lock on interestFilterTable_ is released, so call the callbacks.
				for (int i = 0; i < matchedFilters.Count; ++i) {
					InterestFilterTable.Entry entry = (InterestFilterTable.Entry) matchedFilters[i];
					try {
						entry.getOnInterest().onInterest(
								entry.getFilter().getPrefix(), interest,
								entry.getFace(), entry.getInterestFilterId(),
								entry.getFilter());
					} catch (Exception ex) {
						logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onInterest", ex);
					}
				}
			} else if (data != null) {
				ArrayList pitEntries = new ArrayList();
				pendingInterestTable_.extractEntriesForExpressedInterest(
						data.getName(), pitEntries);
				for (int i_0 = 0; i_0 < pitEntries.Count; ++i_0) {
					PendingInterestTable.Entry pendingInterest = (PendingInterestTable.Entry) pitEntries[i_0];
					try {
						pendingInterest.getOnData().onData(
								pendingInterest.getInterest(), data);
					} catch (Exception ex_1) {
						logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onData", ex_1);
					}
				}
			}
		}
	
		/// <summary>
		/// Check if the face is local based on the current connection through the
		/// Transport; some Transport may cause network IO (e.g. an IP host name lookup).
		/// </summary>
		///
		/// <returns>True if the face is local, false if not.</returns>
		/// <exception cref="IOException"></exception>
		public bool isLocal() {
			return transport_.isLocal(connectionInfo_);
		}
	
		/// <summary>
		/// Shut down by closing the transport
		/// </summary>
		///
		public void shutdown() {
			try {
				transport_.close();
			} catch (IOException e) {
			}
		}
	
		/// <summary>
		/// Get the practical limit of the size of a network-layer packet. If a packet
		/// is larger than this, the library or application MAY drop it.
		/// </summary>
		///
		/// <returns>The maximum NDN packet size.</returns>
		public static int getMaxNdnPacketSize() {
			return net.named_data.jndn.util.Common.MAX_NDN_PACKET_SIZE;
		}
	
		/// <summary>
		/// Call callback.run() after the given delay. This adds to
		/// delayedCallTable_ which is used by processEvents().
		/// </summary>
		///
		/// <param name="delayMilliseconds">The delay in milliseconds.</param>
		/// <param name="callback">This calls callback.run() after the delay.</param>
		public void callLater(double delayMilliseconds, IRunnable callback) {
			delayedCallTable_.callLater(delayMilliseconds, callback);
		}
	
		/// <summary>
		/// Get the next unique entry ID for the pending interest table, interest
		/// filter table, etc. This uses a synchronized to be thread safe. Most entry
		/// IDs are for the pending interest table (there usually are not many interest
		/// filter table entries) so we use a common pool to only have to do the thread
		/// safe lock in one method which is called by Face.
		/// </summary>
		///
		/// <returns>The next entry ID.</returns>
		public long getNextEntryId() {
			 lock (lastEntryIdLock_) {
						return ++lastEntryId_;
					}
		}
	
		/// <summary>
		/// This is used in callLater for when the pending interest expires. If the
		/// pendingInterest is still in the pendingInterestTable_, remove it and call
		/// its onTimeout callback.
		/// </summary>
		///
		/// <param name="pendingInterest">The pending interest to check.</param>
		internal void processInterestTimeout(
				PendingInterestTable.Entry pendingInterest) {
			if (pendingInterestTable_.removeEntry(pendingInterest))
				pendingInterest.callTimeout();
		}
	
		/// <summary>
		/// Do the work of expressInterest once we know we are connected. Add the entry
		/// to the PIT, encode and send the interest.
		/// </summary>
		///
		/// <param name="pendingInterestId"></param>
		/// <param name="interestCopy"></param>
		/// <param name="onData"></param>
		/// <param name="onTimeout"></param>
		/// <param name="wireFormat">A WireFormat object used to encode the message.</param>
		/// <param name="face"></param>
		/// <exception cref="IOException">For I/O error in sending the interest.</exception>
		/// <exception cref="System.Exception">If the encoded interest size exceeds getMaxNdnPacketSize().</exception>
		internal void expressInterestHelper(long pendingInterestId,
				Interest interestCopy, OnData onData, OnTimeout onTimeout,
				WireFormat wireFormat, Face face) {
			PendingInterestTable.Entry pendingInterest = pendingInterestTable_
					.add(pendingInterestId, interestCopy, onData, onTimeout);
			if (onTimeout != null
					|| interestCopy.getInterestLifetimeMilliseconds() >= 0.0d) {
				// Set up the timeout.
				double delayMilliseconds = interestCopy
						.getInterestLifetimeMilliseconds();
				if (delayMilliseconds < 0.0d)
					// Use a default timeout delay.
					delayMilliseconds = 4000.0d;
	
				face.callLater(delayMilliseconds, new Node.Anonymous_C0 (this, pendingInterest));
			}
	
			// Special case: For timeoutPrefix_ we don't actually send the interest.
			if (!timeoutPrefix_.match(interestCopy.getName())) {
				Blob encoding = interestCopy.wireEncode(wireFormat);
				if (encoding.size() > getMaxNdnPacketSize())
					throw new Exception(
							"The encoded interest size exceeds the maximum limit getMaxNdnPacketSize()");
				transport_.send(encoding.buf());
			}
		}
	
		public sealed class Anonymous_C3 : IRunnable {
				private readonly Node outer_Node;
				private readonly OnData onData;
				private readonly long pendingInterestId;
				private readonly Face face;
				private readonly WireFormat wireFormat;
				private readonly Interest interestCopy;
				private readonly OnTimeout onTimeout;
		
				public Anonymous_C3(Node paramouter_Node, OnData onData_0,
						long pendingInterestId_1, Face face_2, WireFormat wireFormat_3,
						Interest interestCopy_4, OnTimeout onTimeout_5) {
					this.onData = onData_0;
					this.pendingInterestId = pendingInterestId_1;
					this.face = face_2;
					this.wireFormat = wireFormat_3;
					this.interestCopy = interestCopy_4;
					this.onTimeout = onTimeout_5;
					this.outer_Node = paramouter_Node;
				}
		
				public void run() {
					try {
						outer_Node.expressInterestHelper(pendingInterestId,
								interestCopy, onData, onTimeout,
								wireFormat, face);
					} catch (IOException ex) {
						net.named_data.jndn.Node.logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, null, ex);
					}
				}
			}
		public sealed class Anonymous_C2 : IRunnable {
				private readonly Node outer_Node;
		
				public Anonymous_C2(Node paramouter_Node) {
					this.outer_Node = paramouter_Node;
				}
		
				public void run() {
					 lock (outer_Node.onConnectedCallbacks_) {
										// Call each callback added while the connection was opening.
										for (int i = 0; i < outer_Node.onConnectedCallbacks_.Count; ++i)
											((IRunnable) outer_Node.onConnectedCallbacks_[i]).run();
										ILOG.J2CsMapping.Collections.Collections.Clear(outer_Node.onConnectedCallbacks_);
						
										// Make future calls to expressInterest send directly to the
										// Transport.
										outer_Node.connectStatus_ = net.named_data.jndn.Node.ConnectStatus.CONNECT_COMPLETE;
									}
				}
			}
		public sealed class Anonymous_C1 : IRunnable {
				private readonly Node outer_Node;
				private readonly long pendingInterestId;
				private readonly Interest interestCopy;
				private readonly Face face;
				private readonly OnTimeout onTimeout;
				private readonly WireFormat wireFormat;
				private readonly OnData onData;
		
				public Anonymous_C1(Node paramouter_Node, long pendingInterestId_0,
						Interest interestCopy_1, Face face_2, OnTimeout onTimeout_3,
						WireFormat wireFormat_4, OnData onData_5) {
					this.pendingInterestId = pendingInterestId_0;
					this.interestCopy = interestCopy_1;
					this.face = face_2;
					this.onTimeout = onTimeout_3;
					this.wireFormat = wireFormat_4;
					this.onData = onData_5;
					this.outer_Node = paramouter_Node;
				}
		
				public void run() {
					try {
						outer_Node.expressInterestHelper(pendingInterestId,
								interestCopy, onData, onTimeout,
								wireFormat, face);
					} catch (IOException ex) {
						net.named_data.jndn.Node.logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, null, ex);
					}
				}
			}
		public sealed class Anonymous_C0 : IRunnable {
				private readonly Node outer_Node;
				private readonly net.named_data.jndn.impl.PendingInterestTable.Entry  pendingInterest;
		
				public Anonymous_C0(Node paramouter_Node, net.named_data.jndn.impl.PendingInterestTable.Entry  pendingInterest_0) {
					this.pendingInterest = pendingInterest_0;
					this.outer_Node = paramouter_Node;
				}
		
				public void run() {
					outer_Node.processInterestTimeout(pendingInterest);
				}
			}
		public enum ConnectStatus {
			UNCONNECTED, CONNECT_REQUESTED, CONNECT_COMPLETE
		}
	
		private class RegisterResponse : OnData, OnTimeout {
			public RegisterResponse(net.named_data.jndn.Node.RegisterResponse.Info  info) {
				info_ = info;
			}
	
			/// <summary>
			/// We received the response.
			/// </summary>
			///
			/// <param name="interest"></param>
			/// <param name="responseData"></param>
			public virtual void onData(Interest interest, Data responseData) {
				// Decode responseData.getContent() and check for a success code.
				ControlResponse controlResponse = new ControlResponse();
				try {
					controlResponse.wireDecode(responseData.getContent(),
							net.named_data.jndn.encoding.TlvWireFormat.get());
				} catch (EncodingException ex) {
					net.named_data.jndn.Node.logger_.log(
							ILOG.J2CsMapping.Util.Logging.Level.INFO,
							"Register prefix failed: Error decoding the NFD response: {0}",
							ex);
					try {
						info_.onRegisterFailed_.onRegisterFailed(info_.prefix_);
					} catch (Exception exception) {
						net.named_data.jndn.Node.logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onRegisterFailed",
								exception);
					}
					return;
				}
	
				// Status code 200 is "OK".
				if (controlResponse.getStatusCode() != 200) {
					net.named_data.jndn.Node.logger_.log(
							ILOG.J2CsMapping.Util.Logging.Level.INFO,
							"Register prefix failed: Expected NFD status code 200, got: {0}",
							controlResponse.getStatusCode());
					try {
						info_.onRegisterFailed_.onRegisterFailed(info_.prefix_);
					} catch (Exception ex_0) {
						net.named_data.jndn.Node.logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onRegisterFailed", ex_0);
					}
					return;
				}
	
				net.named_data.jndn.Node.logger_.log(
						ILOG.J2CsMapping.Util.Logging.Level.INFO,
						"Register prefix succeeded with the NFD forwarder for prefix {0}",
						info_.prefix_.toUri());
				if (info_.onRegisterSuccess_ != null) {
					try {
						info_.onRegisterSuccess_.onRegisterSuccess(info_.prefix_,
								info_.registeredPrefixId_);
					} catch (Exception ex_1) {
						net.named_data.jndn.Node.logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onRegisterSuccess", ex_1);
					}
				}
			}
	
			/// <summary>
			/// We timed out waiting for the response.
			/// </summary>
			///
			/// <param name="timedOutInterest"></param>
			public virtual void onTimeout(Interest timedOutInterest) {
				net.named_data.jndn.Node.logger_.log(ILOG.J2CsMapping.Util.Logging.Level.INFO, "Timeout for NFD register prefix command.");
				try {
					info_.onRegisterFailed_.onRegisterFailed(info_.prefix_);
				} catch (Exception ex) {
					net.named_data.jndn.Node.logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onRegisterFailed", ex);
				}
			}
	
			public class Info {
				
				/// <param name="prefix"></param>
				/// <param name="onRegisterFailed"></param>
				/// <param name="onRegisterSuccess"></param>
				/// <param name="registeredPrefixId"></param>
				public Info(Name prefix, OnRegisterFailed onRegisterFailed,
						OnRegisterSuccess onRegisterSuccess, long registeredPrefixId) {
					prefix_ = prefix;
					onRegisterFailed_ = onRegisterFailed;
					onRegisterSuccess_ = onRegisterSuccess;
					registeredPrefixId_ = registeredPrefixId;
				}
	
				public readonly Name prefix_;
				public readonly OnRegisterFailed onRegisterFailed_;
				public readonly OnRegisterSuccess onRegisterSuccess_;
				public readonly long registeredPrefixId_;
			}
	
			private readonly net.named_data.jndn.Node.RegisterResponse.Info  info_;
		}
	
		/// <summary>
		/// Do the work of registerPrefix to register with NFD.
		/// </summary>
		///
		/// <param name="registeredPrefixId">registeredPrefixTable_ (assuming it has already been done).</param>
		/// <param name="prefix"></param>
		/// <param name="onInterest"></param>
		/// <param name="onRegisterFailed"></param>
		/// <param name="onRegisterSuccess"></param>
		/// <param name="flags"></param>
		/// <param name="commandKeyChain"></param>
		/// <param name="commandCertificateName"></param>
		/// <param name="wireFormat_0"></param>
		/// <param name="face_1"></param>
		/// <exception cref="System.Security.SecurityException">If cannot find the private key for thecertificateName.</exception>
		private void nfdRegisterPrefix(long registeredPrefixId, Name prefix,
				OnInterestCallback onInterest, OnRegisterFailed onRegisterFailed,
				OnRegisterSuccess onRegisterSuccess, ForwardingFlags flags,
				KeyChain commandKeyChain, Name commandCertificateName,
				WireFormat wireFormat_0, Face face_1) {
			if (commandKeyChain == null)
				throw new Exception(
						"registerPrefix: The command KeyChain has not been set. You must call setCommandSigningInfo.");
			if (commandCertificateName.size() == 0)
				throw new Exception(
						"registerPrefix: The command certificate name has not been set. You must call setCommandSigningInfo.");
	
			ControlParameters controlParameters = new ControlParameters();
			controlParameters.setName(prefix);
			controlParameters.setForwardingFlags(flags);
	
			Interest commandInterest = new Interest();
	
			// Determine whether to use remote prefix registration.
			bool faceIsLocal;
			try {
				faceIsLocal = isLocal();
			} catch (IOException ex) {
				logger_.log(
						ILOG.J2CsMapping.Util.Logging.Level.INFO,
						"Register prefix failed: Error attempting to determine if the face is local: {0}",
						ex);
				try {
					onRegisterFailed.onRegisterFailed(prefix);
				} catch (Exception exception) {
					logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onRegisterFailed",
							exception);
				}
				return;
			}
	
			if (faceIsLocal) {
				commandInterest.setName(new Name("/localhost/nfd/rib/register"));
				// The interest is answered by the local host, so set a short timeout.
				commandInterest.setInterestLifetimeMilliseconds(2000.0d);
			} else {
				commandInterest.setName(new Name("/localhop/nfd/rib/register"));
				// The host is remote, so set a longer timeout.
				commandInterest.setInterestLifetimeMilliseconds(4000.0d);
			}
	
			// NFD only accepts TlvWireFormat packets.
			commandInterest.getName().append(
					controlParameters.wireEncode(net.named_data.jndn.encoding.TlvWireFormat.get()));
			makeCommandInterest(commandInterest, commandKeyChain,
					commandCertificateName, net.named_data.jndn.encoding.TlvWireFormat.get());
	
			if (registeredPrefixId != 0) {
				long interestFilterId = 0;
				if (onInterest != null) {
					// registerPrefix was called with the "combined" form that includes the
					// callback, so add an InterestFilterEntry.
					interestFilterId = getNextEntryId();
					setInterestFilter(interestFilterId, new InterestFilter(prefix),
							onInterest, face_1);
				}
	
				registeredPrefixTable_.add(registeredPrefixId, prefix,
						interestFilterId);
			}
	
			// Send the registration interest.
			Node.RegisterResponse  response = new Node.RegisterResponse (
					new RegisterResponse.Info(prefix, onRegisterFailed,
							onRegisterSuccess, registeredPrefixId));
			try {
				expressInterest(getNextEntryId(), commandInterest, response,
						response, wireFormat_0, face_1);
			} catch (IOException ex_2) {
				// Can't send the interest. Call onRegisterFailed.
				logger_.log(
						ILOG.J2CsMapping.Util.Logging.Level.INFO,
						"Register prefix failed: Error sending the register prefix interest to the forwarder: {0}",
						ex_2);
				try {
					onRegisterFailed.onRegisterFailed(prefix);
				} catch (Exception exception_3) {
					logger_.log(ILOG.J2CsMapping.Util.Logging.Level.SEVERE, "Error in onRegisterFailed",
							exception_3);
				}
			}
		}
	
		private readonly Transport transport_;
		private readonly Transport.ConnectionInfo connectionInfo_;
		private readonly PendingInterestTable pendingInterestTable_;
		private readonly InterestFilterTable interestFilterTable_;
		private readonly RegisteredPrefixTable registeredPrefixTable_;
		private readonly DelayedCallTable delayedCallTable_;
		// Use ArrayList without generics so it works with older Java compilers.
		internal readonly IList onConnectedCallbacks_; // Runnable
		private readonly CommandInterestGenerator commandInterestGenerator_;
		private readonly Name timeoutPrefix_;
		private long lastEntryId_;
		private readonly Object lastEntryIdLock_;
		internal Node.ConnectStatus  connectStatus_;
		static internal readonly Logger logger_ = ILOG.J2CsMapping.Util.Logging.Logger
				.getLogger(typeof(Node).FullName);
	}
}
