/**
 * Copyright (C) 2015-2016 Regents of the University of California.
 * @author: Jeff Thompson <jefft0@remap.ucla.edu>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * A copy of the GNU Lesser General Public License is in the file COPYING.
 */

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using ILOG.J2CsMapping.NIO;
using net.named_data.jndn;
using net.named_data.jndn.encoding;
using net.named_data.jndn.util;
using net.named_data.jndn.encoding.der;
using net.named_data.jndn.encrypt.algo;

namespace net.named_data.jndn.util {
  /// <summary>
  /// We set j2cstranslator to not capitalize method names, but it mistakenly doesn't
  /// capitalize method names for .NET classes, so this has extension methods for the
  /// uncapitalized methods to call the capitalized ones.
  /// </summary>
  public static class J2CsExtensions {
    // ArrayList extensions.
    public static int 
    indexOf<T>(this ArrayList<T> array, T value) { return array.IndexOf(value); }

    // Blob extensions.
    public static string
    toString(this Blob blob) { return blob.ToString(); }

    // Enum extensions.
    public static int 
    getNumericType(this ContentType contentType) 
    { 
      return contentType == ContentType.OTHER_CODE ? 0x7fff : (int)contentType; 
    }

    public static int 
    getNumericType(this EncryptAlgorithmType algorithmType) { return (int)algorithmType; }

    public static int 
    getNumericType(this NetworkNack.Reason reason) 
    {
      // The C# enum values are automatically assigned 0, 1, 2, etc. We must be explicit.
      if (reason == NetworkNack.Reason.NONE)
        return 0;
      else if (reason == NetworkNack.Reason.CONGESTION)
        return 50;
      else if (reason == NetworkNack.Reason.DUPLICATE)
        return 100;
      else if (reason == NetworkNack.Reason.NO_ROUTE)
        return 150;
      else if (reason == NetworkNack.Reason.OTHER_CODE)
        return 0x7fff;
      else
        throw new NotImplementedException
          ("getNumericType: Unrecognized NetworkNack.Reason: " + reason);
    }

    // Hashtable extensions.
    public static void 
    clear(this Hashtable map) { map.Clear(); }

    // Random extensions.
    public static void
    nextBytes(this Random random, byte[] array) { random.NextBytes(array); }

    // String extensions.
    public static bool 
    contains(this String str, string value) { return str.Contains(value); }

    public static bool 
    endsWith(this String str, string value) { return str.EndsWith(value); }

    public static bool 
    equals(this String str, string value) { return str.Equals(value); }

    public static int 
    indexOf(this String str, char value) { return str.IndexOf(value); }

    public static int 
    indexOf(this String str, char value, int startIndex) { return str.IndexOf(value, startIndex); }

    public static string
    replace(this String str, string oldValue, string newValue) { return str.Replace(oldValue, newValue); }

    public static string[]
    split(this String str, string regex) { return Regex.Split(str, regex); }

    public static string 
    trim(this String str) { return str.Trim(); }

    // StringBuilder extensions.
    public static StringBuilder 
    append(this StringBuilder builder, char value) 
    { 
      builder.Append(value);
      return builder;
    }

    public static StringBuilder 
    append(this StringBuilder builder, int value) 
    { 
      builder.Append(value);
      return builder;
    }

    public static StringBuilder 
    append(this StringBuilder builder, long value) 
    { 
      builder.Append(value);
      return builder;
    }

    public static StringBuilder 
    append(this StringBuilder builder, string value) 
    { 
      builder.Append(value);
      return builder;
    }

    public static string 
    toString(this StringBuilder builder) { return builder.ToString(); }
  }

  // We need a generic version of ArrayList.
  public class ArrayList<T> : System.Collections.Generic.List<T> {
    public ArrayList() {}

    public ArrayList(IList list)
    {
      foreach (T item in list)
        Add(item);
    }
  }

  public class BufferUnderflowException : Exception {
    public BufferUnderflowException(string message) : base(message) {}
  }

  public class InvalidKeyException : Exception {
    public InvalidKeyException(string message) : base(message) {}
  }

  public class InvalidKeySpecException : Exception {
    public InvalidKeySpecException(string message) : base(message) {}
  }

  public interface IRunnable {
    void run();
  }

  public class ParseException : Exception {
    public ParseException(string message) : base(message) {}
  }

  public class SignatureException : Exception {
    public SignatureException(string message) : base(message) {}
  }

  /// <summary>
  /// This is a simplified implementation of java.text.SimpleDateFormat which 
  /// is hard-wired to UTC time.
  /// </summary>
  public class SimpleDateFormat {
    public SimpleDateFormat(string format) {
      format_ = format; 
    }

    public string
    format(DateTime dateTime) { return dateTime.ToUniversalTime().ToString(format_); }

    public DateTime
    parse(string value) 
    { 
      return DateTime.ParseExact(value, format_, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime(); 
    }

    public void
    setTimeZone(string timeZone)
    {
      // We always use UTC.
      if (timeZone != "UTC")
        throw new NotSupportedException
          ("SimpleDateFormat.setTimeZone does not support timeZone " + timeZone);
    }

    private string format_;
  }

  public class SecureRandom {
    public void nextBytes(byte[] array) { generator_.GetBytes(array); }

    /// <summary>
    /// When when the code calls Next(), it always casts to a byte, so for
    /// simplicity just return a byte.
    /// </summary>
    public byte Next() 
    {
      var result = new byte[1];
      generator_.GetBytes(result);
      return result[0];
    }

    private static RNGCryptoServiceProvider generator_ = new RNGCryptoServiceProvider();
  }
}

/// <summary>
/// j2cstranslator makes naive assumptions and puts some Java classes into the System
/// name space, so we have to pollute the System name space with them.
/// </summary>
namespace System {
  /// <summary>
  /// j2cstranslator naively converts java.security.KeyFactory to System.KeyFactory.
  /// </summary>
  public abstract class KeyFactory {
    public static KeyFactory
    getInstance(string type)
    {
      if (type == "RSA")
        return new RsaKeyFactory();
      else
        throw new NotImplementedException("KeyFactory type is not implemented: " + type);
    }

    public abstract PrivateKey
    generatePrivate(KeySpec keySpec);

    public abstract SecurityPublicKey
    generatePublic(KeySpec keySpec);
  }

  public class RsaKeyFactory : KeyFactory {
    public override PrivateKey
    generatePrivate(KeySpec keySpec)
    {
      if (!(keySpec is PKCS8EncodedKeySpec))
        throw new net.named_data.jndn.util.InvalidKeySpecException
          ("RsaKeyFactory.generatePrivate expects a PKCS8EncodedKeySpec");

      try {
        // Decode the PKCS #8 private key.
        var parsedNode = DerNode.parse(new ByteBuffer(((PKCS8EncodedKeySpec)keySpec).KeyDer), 0);
        var pkcs8Children = parsedNode.getChildren();
        var algorithmIdChildren = DerNode.getSequence(pkcs8Children, 1).getChildren();
        var oidString = ((DerNode.DerOid)algorithmIdChildren[0]).toVal().ToString();
        var rsaPrivateKeyDer = ((DerNode)pkcs8Children[2]).getPayload();

        if (oidString != RSA_ENCRYPTION_OID)
          throw new net.named_data.jndn.util.InvalidKeySpecException
            ("The PKCS #8 private key is not RSA_ENCRYPTION");

        // Decode the PKCS #1 RSAPrivateKey.
        parsedNode = DerNode.parse(rsaPrivateKeyDer.buf(), 0);
        var rsaPrivateKeyChildren = parsedNode.getChildren();

        // Copy the parameters.
        RSAParameters parameters = new RSAParameters();
        var modulus = getIntegerArrayWithoutLeadingZero(((DerNode)rsaPrivateKeyChildren[1]).getPayload());
        parameters.Modulus = modulus;
        parameters.Exponent = getIntegerArrayWithoutLeadingZero(((DerNode)rsaPrivateKeyChildren[2]).getPayload());
        // RSAParameters expects the integer array of the correct length.
        parameters.D = getIntegerArrayOfSize(((DerNode)rsaPrivateKeyChildren[3]).getPayload(), modulus.Length);
        parameters.P = getIntegerArrayOfSize(((DerNode)rsaPrivateKeyChildren[4]).getPayload(), modulus.Length / 2);
        parameters.Q = getIntegerArrayOfSize(((DerNode)rsaPrivateKeyChildren[5]).getPayload(), modulus.Length / 2);
        parameters.DP = getIntegerArrayOfSize(((DerNode)rsaPrivateKeyChildren[6]).getPayload(), modulus.Length / 2);
        parameters.DQ = getIntegerArrayOfSize(((DerNode)rsaPrivateKeyChildren[7]).getPayload(), modulus.Length / 2);
        parameters.InverseQ = getIntegerArrayOfSize(((DerNode)rsaPrivateKeyChildren[8]).getPayload(), modulus.Length / 2);

        return new RsaSecurityPrivateKey(parameters);
      } catch (DerDecodingException ex) {
        throw new net.named_data.jndn.util.InvalidKeySpecException
          ("RsaKeyFactory.generatePrivate error decoding the private key DER: " + ex);
      }
    }

    public override SecurityPublicKey
    generatePublic(KeySpec keySpec)
    {
      if (!(keySpec is X509EncodedKeySpec))
        throw new net.named_data.jndn.util.InvalidKeySpecException
        ("RsaKeyFactory.generatePublic expects a X509EncodedKeySpec");

      try {
        // Decode the X.509 public key.
        var parsedNode = DerNode.parse(new ByteBuffer(((X509EncodedKeySpec)keySpec).KeyDer), 0);
        var rootChildren = parsedNode.getChildren();
        var algorithmIdChildren = DerNode.getSequence(rootChildren, 0).getChildren();
        var oidString = ((DerNode.DerOid)algorithmIdChildren[0]).toVal().ToString();
        var rsaPublicKeyDerBitString = ((DerNode)rootChildren[1]).getPayload();

        if (oidString != RSA_ENCRYPTION_OID)
          throw new net.named_data.jndn.util.InvalidKeySpecException
          ("The PKCS #8 private key is not RSA_ENCRYPTION");

        // Decode the PKCS #1 RSAPublicKey.
        // Skip the leading 0 byte in the DER BitString.
        parsedNode = DerNode.parse(rsaPublicKeyDerBitString.buf(), 1);
        var rsaPublicKeyChildren = parsedNode.getChildren();

        // Copy the parameters.
        RSAParameters parameters = new RSAParameters();
        parameters.Modulus = getIntegerArrayWithoutLeadingZero(((DerNode)rsaPublicKeyChildren[0]).getPayload());
        parameters.Exponent = getIntegerArrayWithoutLeadingZero(((DerNode)rsaPublicKeyChildren[1]).getPayload());

        return new RsaSecurityPublicKey(parameters);
        } catch (DerDecodingException ex) {
          throw new net.named_data.jndn.util.InvalidKeySpecException
          ("RsaKeyFactory.generatePublic error decoding the public key DER: " + ex);
      }
    }

    /// <summary>
    /// A Der Integer is signed which means it can have a leading zero, but we need
    /// to strip the leading zero to use it in an RSAParameters.
    /// </summary>
    /// <returns>The array without leading a zero.</returns>
    /// <param name="integer">The DER Integer payload.</param>
    public static byte[]
    getIntegerArrayWithoutLeadingZero(Blob integer)
    {
      var buffer = integer.buf();
      if (buffer.get(buffer.position()) == 0)
        return getIntegerArrayOfSize(integer, buffer.remaining() - 1);
      else
        return integer.getImmutableArray();
    }

    /// <summary>
    /// Strip leading zeros until the integer Blob has the given size. This is
    /// necessary because RSAParameters expects integer byte arrays of a given
    /// size based on the size of the modulus.
    /// </summary>
    /// <returns>The array of the given size.</returns>
    /// <param name="integer">The DER Integer payload.</param>
    /// <param name="size">The number of bytes.</param>
    public static byte[]
    getIntegerArrayOfSize(Blob integer, int size)
    {
      var buffer = integer.buf();
      while (buffer.remaining() > size) {
        if (buffer.get(buffer.position()) != 0)
          throw new Exception("getIntegerArrayOfSize: The leading byte to strip is not zero");
        buffer.position(buffer.position() + 1);
      }

      // If position was unchanged, this does not copy.
      return new Blob(buffer, false).getImmutableArray();
    }

    /// <summary>
    /// Return the integer byte array as a ByteBuffer, prepending a zero byte if
    /// the first byte of the integer is >= 0x80.
    /// </summary>
    /// <returns>The positive integer buffer.</returns>
    /// <param name="integer">The integer byte array. If this doesn't prepend a zero,
    /// then this just returns ByteBuffer.wrap(integer).</param>
    public static ByteBuffer
    getPositiveIntegerBuffer(byte[] integer)
    {
      if (integer.Length == 0 || integer[0] < 0x80)
        return ByteBuffer.wrap(integer);

      var result = ByteBuffer.allocate(integer.Length + 1);
      result.put((byte)0);
      result.put(integer);
      result.flip();
      return result;
    }

    public static string RSA_ENCRYPTION_OID = "1.2.840.113549.1.1.1";
  }

  /// <summary>
  /// j2cstranslator naively converts java.security.KeyPair to System.KeyPair.
  /// </summary>
  public class KeyPair {
    public KeyPair(SecurityPublicKey publicKey, PrivateKey privateKey)
    {
      publicKey_ = publicKey;
      privateKey_ = privateKey;
    }

    public SecurityPublicKey
    getPublic() { return publicKey_; }

    public PrivateKey
    getPrivate() { return privateKey_; }

    private SecurityPublicKey publicKey_;
    private PrivateKey privateKey_;
  }

  /// <summary>
  /// j2cstranslator naively converts java.security.KeyPairGenerator to System.KeyPairGenerator.
  /// </summary>
  public abstract class KeyPairGenerator {
    public static KeyPairGenerator 
    getInstance(string type)
    {
      if (type == "RSA")
        return new RsaKeyPairGenerator();
      else
        throw new NotImplementedException("KeyPairGenerator type is not implemented: " + type);
    }

    public abstract void
    initialize(int keySize);

    public abstract KeyPair
    generateKeyPair();
  }

  public class RsaKeyPairGenerator : KeyPairGenerator {
    public override void
    initialize(int keySize)
    {
      keySize_ = keySize;
    }

    public override KeyPair
    generateKeyPair()
    {
      var parameters = new RSACryptoServiceProvider(keySize_).ExportParameters(true);
      return new KeyPair
        (new RsaSecurityPublicKey(parameters), new RsaSecurityPrivateKey(parameters));
    }

    private int keySize_;
  }

  /// <summary>
  /// j2cstranslator naively converts java.security.KeySpec to System.KeySpec.
  /// </summary>
  public interface KeySpec {
  }

  public class PKCS8EncodedKeySpec : KeySpec {
    public PKCS8EncodedKeySpec(byte[] keyDer)
    {
      KeyDer = keyDer;
    }

    public readonly byte[] KeyDer;
  }

  public class X509EncodedKeySpec : KeySpec {
    public X509EncodedKeySpec(byte[] keyDer)
    {
      KeyDer = keyDer;
    }

    public readonly byte[] KeyDer;
  }

  /// <summary>
  /// j2cstranslator naively converts java.security.PrivateKey to System.PrivateKey.
  /// </summary>
  public abstract class PrivateKey {
    public abstract byte[]
    getEncoded();
  }

  public class RsaSecurityPrivateKey : PrivateKey {
    /// <summary>
    /// Create an RsaPrivateKey with the RSAParameters used by RSACryptoServiceProvider.
    /// </summary>
    /// <param name="parameters">Parameters.</param>
    public RsaSecurityPrivateKey(RSAParameters parameters)
    {
      Parameters = parameters;
    }

    public override byte[]
    getEncoded()
    {
      // First encode an PKCS #1 RSAPrivateKey.
      var rsaPrivateKey = new DerNode.DerSequence();
      rsaPrivateKey.addChild(new DerNode.DerInteger(0));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.Modulus)));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.Exponent)));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.D)));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.P)));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.Q)));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.DP)));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.DQ)));
      rsaPrivateKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.InverseQ)));

      // Encode rsaPrivateKey as a PKCS #8 private key.
      var algorithmIdentifier = new DerNode.DerSequence();
      algorithmIdentifier.addChild(new DerNode.DerOid(new OID(RsaKeyFactory.RSA_ENCRYPTION_OID)));
      algorithmIdentifier.addChild(new DerNode.DerNull());

      var privateKey = new DerNode.DerSequence();
      privateKey.addChild(new DerNode.DerInteger(0));
      privateKey.addChild(algorithmIdentifier);
      privateKey.addChild(new DerNode.DerOctetString(rsaPrivateKey.encode().buf()));

      return privateKey.encode().getImmutableArray();
    }

    public readonly RSAParameters Parameters;
  }

  /// <summary>
  /// j2cstranslator naively converts java.security.PublicKey to System.SecurityPublicKey.
  /// We also globally rename System.SecurityPublicKey to System.SecurityPublicKey to not
  /// conclict with PublicKey when using net.named_data.security.certificate.
  /// </summary>
  public abstract class SecurityPublicKey {
    public abstract byte[]
    getEncoded();
  }

  public class RsaSecurityPublicKey : SecurityPublicKey {
    /// <summary>
    /// Create an RsaSecurityPublicKey with the RSAParameters used by RSACryptoServiceProvider.
    /// </summary>
    /// <param name="parameters">Parameters.</param>
    public RsaSecurityPublicKey(RSAParameters parameters)
    {
      Parameters = parameters;
    }

    public override byte[]
    getEncoded()
    {
      // First encode an PKCS #1 RSAPublicKey.
      var rsaPublicKey = new DerNode.DerSequence();
      rsaPublicKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.Modulus)));
      rsaPublicKey.addChild(new DerNode.DerInteger
        (RsaKeyFactory.getPositiveIntegerBuffer(Parameters.Exponent)));

      // Encode rsaPublicKey as an X.509 public key.
      var algorithmIdentifier = new DerNode.DerSequence();
      algorithmIdentifier.addChild(new DerNode.DerOid(new OID(RsaKeyFactory.RSA_ENCRYPTION_OID)));
      algorithmIdentifier.addChild(new DerNode.DerNull());

      var publicKey = new DerNode.DerSequence();
      publicKey.addChild(algorithmIdentifier);
      publicKey.addChild(new DerNode.DerBitString(rsaPublicKey.encode().buf(), 0));

      return publicKey.encode().getImmutableArray();
    }

    public readonly RSAParameters Parameters;
  }

  /// <summary>
  /// j2cstranslator naively converts java.security.Signature to System.SecuritySignature.
  /// We also globally rename System.SecuritySignature to System.SecuritySignature to not
  /// conclict with Signature when using net.named_data.jndn.
  /// </summary>
  public abstract class SecuritySignature {
    public static SecuritySignature 
    getInstance(string type)
    {
      if (type == "SHA256withRSA")
        return new Sha256withRsaSecuritySignature();
      else
        throw new NotImplementedException("SecuritySignature type is not implemented: " + type);
    }

    public abstract void
    initSign(PrivateKey privateKey);

    public abstract void
    initVerify(SecurityPublicKey publicKey);

    public abstract byte[]
    sign();

    public abstract void
    update(ByteBuffer data);

    public abstract bool
    verify(byte[] signature);
  }

  public class Sha256withRsaSecuritySignature : SecuritySignature {
    public override void
    initSign(PrivateKey privateKey)
    {
      if (!(privateKey is RsaSecurityPrivateKey))
        throw new net.named_data.jndn.util.InvalidKeyException
        ("Sha256withRsaSecuritySignature.initSign expects an RsaSecurityPrivateKey");

      provider_ = new RSACryptoServiceProvider();
      provider_.ImportParameters(((RsaSecurityPrivateKey)privateKey).Parameters);

      memoryStream_ = new MemoryStream();
    }

    public override void
    initVerify(SecurityPublicKey publicKey)
    {
      if (!(publicKey is RsaSecurityPublicKey))
        throw new net.named_data.jndn.util.InvalidKeyException
        ("Sha256withRsaSecuritySignature.initVerify expects an RsaSecurityPublicKey");

      provider_ = new RSACryptoServiceProvider();
      provider_.ImportParameters(((RsaSecurityPublicKey)publicKey).Parameters);

      memoryStream_ = new MemoryStream();
    }

    public override byte[]
    sign()
    {
      memoryStream_.Flush();
      var result = provider_.SignData(memoryStream_.ToArray(), new SHA256CryptoServiceProvider());

      // We don't need the data in the stream any more.
      memoryStream_.Dispose();
      memoryStream_ = null;

      return result;
    }
      
    public override void
    update(ByteBuffer data)
    {
      memoryStream_.Write(data.array(), data.arrayOffset(), data.remaining());
    }

    public override bool
    verify(byte[] signature)
    {
      memoryStream_.Flush();
      var result = provider_.VerifyData
        (memoryStream_.ToArray(), new SHA256CryptoServiceProvider(), signature);

      // We don't need the data in the stream any more.
      memoryStream_.Dispose();
      memoryStream_ = null;

      return result;
    }

    private RSACryptoServiceProvider provider_;
    private MemoryStream memoryStream_;
  }
}

namespace System.Security.Cryptography {
  /// <summary>
  /// j2cstranslator naively converts java.security.MessageDigest to MD5 (!) and uses the
  /// Java interface. The INSTALL file instructions replace MD5 with this class.
  /// </summary>
  class SecuritySHA256 {
    public static SecuritySHA256
    Create() { return new SecuritySHA256(); }

    public void
    update(ByteBuffer data)
    {
      memoryStream_.Write(data.array(), data.arrayOffset(), data.remaining());
    }

    public byte[] Hash {
      get { 
        memoryStream_.Flush();
        var result = sha256_.ComputeHash(memoryStream_.ToArray());

        // We don't need the data in the stream any more.
        memoryStream_.Dispose();
        memoryStream_ = null;

        return result;
      }
    }

    private SHA256 sha256_ = SHA256Managed.Create();
    private MemoryStream memoryStream_ = new MemoryStream();
  }
}

namespace System.Collections {
  /// <summary>
  /// This is used with SimpleDateFormat.
  /// </summary>
  public class TimeZone {
    public static string
    getTimeZone(string timeZone)
    {
      if (timeZone == "UTC")
        return timeZone;
      else
        throw new NotSupportedException
          ("TimeZone.getTimeZone does not support timeZone " + timeZone);
    }
  }
}

namespace System.spec {
}

namespace javax.crypto {
  public abstract class Mac {
    public static Mac 
    getInstance(string algorithm)
    {
      if (algorithm == "HmacSHA256")
        return new HmacSHA256Mac();
      else
        throw new NotSupportedException
          ("Mac.getInstance does not algorithm " + algorithm);
    }

    public abstract void 
    init(Key keySpec);

    public abstract void 
    update(ByteBuffer data);

    public abstract byte[] 
    doFinal();

    class HmacSHA256Mac : Mac {
      public override void
      init(Key key)
      {
        if (!(key is javax.crypto.spec.SecretKeySpec))
          throw new net.named_data.jndn.util.InvalidKeyException
            ("HmacSHA256Mac.init expects a SecretKeySpec");

        key_ = ((javax.crypto.spec.SecretKeySpec)key).Key;
      }

      public override void 
      update(ByteBuffer data)
      {
        memoryStream_.Write(data.array(), data.arrayOffset(), data.remaining());
      }

      public override byte[] 
      doFinal()
      {
        using (var hmac = new HMACSHA256(key_)) {
          memoryStream_.Flush();
          var result = hmac.ComputeHash(memoryStream_.ToArray());

          // We don't need the data in the stream any more.
          memoryStream_.Dispose();
          memoryStream_ = null;
          return result;
        }
      }

      private byte[] key_;
      private MemoryStream memoryStream_ = new MemoryStream();
    }
  }

  public interface Key {
  }

  public interface SecretKey : Key {
  }
}

namespace javax.crypto.spec {
  public class SecretKeySpec : KeySpec, SecretKey {
    public SecretKeySpec(byte[] key, string algorithm)
    {
      Key = key;
    }

    public readonly byte[] Key;
  }
}
