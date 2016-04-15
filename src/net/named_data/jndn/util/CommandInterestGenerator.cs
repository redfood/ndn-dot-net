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
namespace net.named_data.jndn.util {
	
	using ILOG.J2CsMapping.NIO;
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using net.named_data.jndn;
	using net.named_data.jndn.encoding;
	using net.named_data.jndn.encoding.tlv;
	using net.named_data.jndn.security;
	
	/// <summary>
	/// A CommandInterestGenerator keeps track of a timestamp and generates
	/// command interests according to the NFD Signed Command Interests protocol:
	/// http://redmine.named-data.net/projects/nfd/wiki/Command_Interests
	/// </summary>
	///
	public class CommandInterestGenerator {
		/// <summary>
		/// Create a new CommandInterestGenerator and initialize the timestamp to now.
		/// </summary>
		///
		public CommandInterestGenerator() {
			this.lastTimestampLock_ = new Object();
			lastTimestamp_ = Math.Round(net.named_data.jndn.util.Common.getNowMilliseconds(),MidpointRounding.AwayFromZero);
		}
	
		/// <summary>
		/// Append a timestamp component and a random value component to interest's
		/// name. This ensures that the timestamp is greater than the timestamp used in
		/// the previous call. Then use keyChain to sign the interest which appends a
		/// SignatureInfo component and a component with the signature bits. If the
		/// interest lifetime is not set, this sets it.
		/// </summary>
		///
		/// <param name="interest">The interest whose name is append with components.</param>
		/// <param name="keyChain">The KeyChain for calling sign.</param>
		/// <param name="certificateName">The certificate name of the key to use for signing.</param>
		/// <param name="wireFormat"></param>
		public void generate(Interest interest, KeyChain keyChain,
				Name certificateName, WireFormat wireFormat) {
			double timestamp;
			 lock (lastTimestampLock_) {
						timestamp = Math.Round(net.named_data.jndn.util.Common.getNowMilliseconds(),MidpointRounding.AwayFromZero);
						while (timestamp <= lastTimestamp_)
							timestamp += 1.0d;
						// Update the timestamp now while it is locked. In the small chance that
						//   signing fails, it just means that we have bumped the timestamp.
						lastTimestamp_ = timestamp;
					}
	
			// The timestamp is encoded as a TLV nonNegativeInteger.
			TlvEncoder encoder = new TlvEncoder(8);
			encoder.writeNonNegativeInteger((long) timestamp);
			interest.getName().append(new Blob(encoder.getOutput(), false));
	
			// The random value is a TLV nonNegativeInteger too, but we know it is 8 bytes,
			//   so we don't need to call the nonNegativeInteger encoder.
			ByteBuffer randomBuffer = ILOG.J2CsMapping.NIO.ByteBuffer.allocate(8);
			// Note: SecureRandom is thread safe.
			net.named_data.jndn.util.Common.getRandom().nextBytes(randomBuffer.array());
			interest.getName().append(new Blob(randomBuffer, false));
	
			keyChain.sign(interest, certificateName, wireFormat);
	
			if (interest.getInterestLifetimeMilliseconds() < 0)
				// The caller has not set the interest lifetime, so set it here.
				interest.setInterestLifetimeMilliseconds(1000.0d);
		}
	
		/// <summary>
		/// Append a timestamp component and a random value component to interest's
		/// name. This ensures that the timestamp is greater than the timestamp used in
		/// the previous call. Then use keyChain to sign the interest which appends a
		/// SignatureInfo component and a component with the signature bits. If the
		/// interest lifetime is not set, this sets it. Use the default WireFormat to
		/// encode the SignatureInfo and to encode interest name for signing.
		/// </summary>
		///
		/// <param name="interest">The interest whose name is append with components.</param>
		/// <param name="keyChain">The KeyChain for calling sign.</param>
		/// <param name="certificateName">The certificate name of the key to use for signing.</param>
		public void generate(Interest interest, KeyChain keyChain,
				Name certificateName) {
			generate(interest, keyChain, certificateName,
					net.named_data.jndn.encoding.WireFormat.getDefaultWireFormat());
		}
	
		private double lastTimestamp_;
		private readonly Object lastTimestampLock_;
	}
}
