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

/// <summary>
/// The j2cstranslator uses auxiliary methods in the ILOG.J2CsMapping, but the
/// DLL doesn't load in Unity, so this file is a stub with the needed methods.
/// </summary>

using System;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ILOG.J2CsMapping.Collections {
  public class Arrays {
    public static bool
    Equals(byte[] array1, byte[] array2)
    {
      if (array1.Length != array2.Length)
        return false;

      for (var i = 0; i < array1.Length; ++i) {
        if (array1[i] != array2[i])
          return false;
      }
      return true;
    }
  }

  public class Collections {
    public static void 
    Add(IList list, object value) { list.Add(value); }

    public static void 
    Clear(IList list) { list.Clear(); }

    public static object
    Get(Hashtable map, object key) { return map[key]; }

    public static void
    Put(Hashtable map, object key, object value) { map[key] = value; }

    public static object 
    Remove(Hashtable map, object key) 
    {
      // Imitate Java HashMap.remove.
      object oldValue = null;
      if (map.ContainsKey(key))
        oldValue = map[key];
      
      map.Remove(key); 
      return oldValue;
    }

    public static bool 
    Remove(IList list, object entry)
    { 
      // Imitate Java List.remove.
      bool hadValue = list.Contains(entry);
      list.Remove(entry); 
      return hadValue;
    }

    public static void 
    RemoveAt(IList list, int index) { list.RemoveAt(index); }

    public static IList synchronizedList(IList list) { return list; }
  }
}

namespace ILOG.J2CsMapping.NIO {
  /// <summary>
  /// Mimic of Java.nio.ByteBuffer class. Backed by a byte array, ByteBuffer has several 
  /// integers pointing to positions of different meaning. Whenever trying to read 
  /// something from a ByteBuffer, (flip it to reading mode if it's in writing mode), 
  /// use the get method provided by this class. If an array is used directly, the valid 
  /// data for this class starts from array_[arrayOffset_ + position_], and ends with 
  /// array_[arrayOffset_ + limit_], with the total length of (remaining_ = limit_ - position_).
  /// The mark member is not implemented in this mimic.
  /// </summary>
  public class ByteBuffer : IComparable {
    public ByteBuffer(byte[] array) 
    {
      array_ = array;
      arrayOffset_ = 0;
      capacity_ = array_.Length;
      position_ = 0;
      limit_ = capacity_;
    }

    private ByteBuffer(ByteBuffer byteBuffer) 
    {
      array_ = byteBuffer.array_;
      arrayOffset_ = byteBuffer.arrayOffset_;
      capacity_ = byteBuffer.capacity_;
      position_ = byteBuffer.position_;
      limit_ = byteBuffer.limit_;
    }

    public static ByteBuffer 
    allocate(int capacity) 
    {
      return new ByteBuffer(new byte[capacity]);
    }

    public static ByteBuffer 
    wrap(byte[] array) 
    {
      return new ByteBuffer(array);
    }

    public ByteBuffer 
    asReadOnlyBuffer() 
    {
      // TODO: Set a read-only flag.
      return new ByteBuffer(this);
    }

    public bool 
    isReadOnly() 
    {
      // TODO: Set a read-only flag.
      return false;
    }

    public byte[] 
    array() 
    {
      return array_;
    }

    public int 
    arrayOffset() 
    {
      return arrayOffset_;
    }

    public int 
    capacity() 
    {
      return capacity_;
    }

    public ByteBuffer 
    duplicate() 
    {
      return new ByteBuffer(this);
    }

    /// <summary>
    /// Flips the ByteBuffer from 'write into' mode to 'read from' mode: 
    /// limit is set to position while position is set to 0.
    /// </summary>
    public void 
    flip() 
    {
      limit_ = position_;
      position_ = 0;
    }

    /// <summary>
    /// Get the next byte in the ByteBuffer, and update the position.
    /// </summary>
    public byte 
    get() 
    {
      if (position_ >= limit_)
        throw new Exception(
          "ByteBuffer.get: position() must be < limit().");

      return get(position_++);
    }

    /// <summary>
    /// This method transfers bytes from this ByteBuffer into the given destination array. 
    /// If there are fewer bytes remaining in this ByteBuffer than are required to satisfy the 
    /// request, that is, if length > remaining(), then no bytes are transferred and an exception 
    /// is thrown. Otherwise, this method copies length bytes from this ByteBuffer into the given array, 
    /// starting at the current position of this ByteBuffer and at the given offset in the array. 
    /// The position of this ByteBuffer is then incremented by length. 
    /// </summary>
    /// <param name="array">The array to receive the bytes.</param>
    /// <param name="offset">The starting offset in array to receive bytes.</param>
    /// <param name="length">The number of bytes to copy to the array.</param>
    /// <returns>This ByteBuffer.</returns>
    public ByteBuffer 
    get(byte[] array, int offset, int length)
    {
      if (length > remaining())
        throw new IndexOutOfRangeException
          ("ByteBuffer.get: there are fewer bytes remaining in this ByteBuffer than are required to satisfy the request");

      Buffer.BlockCopy(array_, arrayOffset_ + position_, array, offset, length);
      position_ += length;
      return this;
    }

    /// <summary>
    /// This method transfers bytes from this ByteBuffer into the given destination array. 
    /// An invocation of this method of the form src.get(a) behaves in exactly the same 
    /// way as the invocation src.get(a, 0, a.length). The position of this ByteBuffer 
    /// is incremented by a.length. 
    /// </summary>
    /// <param name="array">The array to receive the bytes.</param>
    /// <returns>This ByteBuffer.</returns>
    public ByteBuffer 
    get(byte[] array)
    {
      return get(array, 0, array.Length);
    }

    public byte 
    get(int index) 
    {
      if (index < 0) {
        Console.WriteLine ("ByteBuffer.get: Index negative");
        throw new ArgumentException ("ByteBuffer.get: index cannot be negative.");
      }
      if (index >= capacity_) {
        Console.WriteLine ("ByteBuffer.get: Index larger than capacity");
        throw new ArgumentException ("ByteBuffer.get: index cannot be >= capacity().");
      }

      return array_[arrayOffset_ + index];
    }

    public bool 
    hasRemaining() 
    {
      return position_ < limit_;
    }

    public int 
    limit() 
    {
      return limit_;
    }

    public void 
    limit(int newLimit) 
    {
      if (newLimit < 0)
        throw new ArgumentException(
          "ByteBuffer.limit: newLimit cannot be negative.");
      if (newLimit > capacity_)
        throw new ArgumentException(
          "ByteBuffer.limit: newLimit cannot be > capacity().");

      limit_ = newLimit;
      if (position_ > newLimit)
        position_ = newLimit;
    }

    public int 
    position() 
    {
      return position_;
    }

    /// <summary>
    /// Sets the position_ of ByteBuffer to a given integer.
    /// </summary>
    /// <param name="newPosition">Index of the new position.</param>
    public void 
    position(int newPosition) 
    {
      if (newPosition < 0)
        throw new ArgumentException(
          "ByteBuffer.position: newPosition cannot be negative.");
      if (newPosition > limit_)
        throw new ArgumentException(
          "ByteBuffer.position: newPosition cannot be > limit().");

      position_ = newPosition;
    }

    public ByteBuffer 
    put(byte b) 
    {
      if (position_ >= limit_)
        throw new Exception(
          "ByteBuffer.put: position() must be < limit().");

      return put(position_++, b);
    }

    public ByteBuffer 
    put(int index, byte b) 
    {
      if (index < 0)
        throw new ArgumentException(
          "ByteBuffer.put: index cannot be negative.");
      if (index >= capacity_)
        throw new ArgumentException(
          "ByteBuffer.put: index cannot be >= capacity().");

      array_[arrayOffset_  + index] = b;
      return this;
    }

    public ByteBuffer 
    put(byte[] src, int offset, int length) 
    {
      // TODO: Implement efficiently.
      for (int i = offset; i < offset + length; ++i)
        put(src[i]);

      return this;
    }

    public ByteBuffer 
    put(byte[] src) 
    {
      return put(src, 0, src.Length);
    }

    public ByteBuffer 
    put(ByteBuffer src) 
    {
      // TODO: Implement efficiently.
      while (src.hasRemaining())
        put(src.get());
      return this;
    }

    /// <summary>
    /// Determines whether the specified <see cref="net.named_data.jndn.ByteBuffer"/> is 
    /// equal to the current <see cref="net.named_data.jndn.ByteBuffer"/>. Method overriden.
    /// </summary>
    /// <param name="byteBuffer">The <see cref="net.named_data.jndn.ByteBuffer"/> to 
    /// compare with the current <see cref="net.named_data.jndn.ByteBuffer"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="net.named_data.jndn.ByteBuffer"/> is equal to the current
    /// <see cref="net.named_data.jndn.ByteBuffer"/>; otherwise, <c>false</c>.</returns>
    public bool 
    equals(ByteBuffer byteBuffer)
    {
      if (byteBuffer.remaining() != remaining())
        return false;
      int j = byteBuffer.arrayOffset_ + byteBuffer.position_;
      for (int i = arrayOffset_ + position_; i < arrayOffset_ + limit_; ++i, ++j) {
        if (byteBuffer.array_[j] != array_[i])
          return false;
      }

      return true;
    }
      
    public override bool Equals(Object other) {
      if (!(other is ByteBuffer))
        return false;

      return equals((ByteBuffer)other);
    }

    public int 
    remaining() 
    {
      return limit_ - position_;
    }

    public ByteBuffer 
    slice() 
    {
      ByteBuffer result = new ByteBuffer(this);
      // result.get(0) returns this.get(position_).
      result.position_ = 0;
      result.arrayOffset_ = arrayOffset_ + position_;
      result.capacity_ = remaining();
      result.limit_ = remaining();

      return result;
    }

    public int 
    compareTo(ByteBuffer other)
    {
      throw new NotImplementedException("ByteBuffer.compareTo is not implemented");
    }

    public int 
    CompareTo(Object other) {
      return compareTo((ByteBuffer)other);
    }

    private byte[] array_;
    private int arrayOffset_; // get(0) returns array_[arrayOffset_].
    private int capacity_;

    private int position_;
    private int limit_;
  }
}

namespace ILOG.J2CsMapping.Reflect {
  public class Helper {
    public static MethodInfo
    GetMethod(Type type, string name, Type arg1Type) 
    { 
      return type.GetMethod(name, new Type[] { arg1Type });
    }

    public static MethodInfo
    GetMethod(Type type, string name, Type arg1Type, Type arg2Type) 
    { 
      return type.GetMethod(name, new Type[] { arg1Type, arg2Type });
    }

    public static Type
    GetNativeType(String type) 
    { 
      if (type == "javax.xml.bind.DatatypeConverter")
        // This is used in Common.establishBase64Converter.
        return typeof(DatatypeConverter);
      else
        throw new NotSupportedException
          ("Reflect.Helper.GetNativeType does not support type " + type);
    }

    public static object
    Invoke(MethodInfo methodInfo, object obj, object arg1) 
    { 
      return methodInfo.Invoke(obj, new object[] { arg1 });
    }

    public static object
    Invoke(MethodInfo methodInfo, object obj, object arg1, object arg2) 
    { 
      return methodInfo.Invoke(obj, new object[] { arg1, arg2 });
    }
  }

  /// <summary>
  /// This emulates javax.xml.bind.DatatypeConverter.
  /// These methods are invoked by Common.base64Encode and base64Decode.
  /// </summary>
  public class DatatypeConverter {
    public static string
    printBase64Binary(byte[] array) { return System.Convert.ToBase64String(array); }

    public static byte[]
    parseBase64Binary(string encoding) { return System.Convert.FromBase64String(encoding); }
  }
}

namespace ILOG.J2CsMapping.Text {
  public class Matcher {
    public Matcher(Match match)
    {
      match_ = match;
    }

    public bool
    Find() 
    {
      if (!didFirstFind_)
        // We already tried to match.
        didFirstFind_ = true;
      else
        match_ = match_.NextMatch();
      
      return match_.Success;
    }

    public int
    start(int groupNumber) { return match_.Groups[groupNumber].Index; }

    public int
    end(int groupNumber) 
    { 
      return match_.Groups[groupNumber].Index + match_.Groups[groupNumber].Length; 
    }

    public string
    Group(int groupNumber) { return match_.Groups[groupNumber].Value; }

    private Match match_;
    private bool didFirstFind_ = false;
  }

  public class Pattern {
    private Pattern(Regex regex)
    {
      regex_ = regex;
    }

    public static Pattern
    Compile(string pattern) { return new Pattern(new Regex(pattern)); }

    public Matcher
    Matcher(string text) { return new Matcher(regex_.Match(text)); }

    private Regex regex_;
  }

  public class RegExUtil {
    public static string[]
    Split(String str, string regex) { return Regex.Split(str, regex); }
  }
}

namespace ILOG.J2CsMapping.Util {
  public class Calendar {
    private Calendar(DateTime dateTime)
    {
      dateTime_ = dateTime;
    }

    public static readonly int YEAR = 1;

    public static  Calendar
    getInstance() { return new Calendar(DateTime.Now); }

    public double
    getTimeInMillis() { return (dateTime_ - epoch_).TotalMilliseconds; }

    public void
    add(int field, int amount)
    {
      if (field == YEAR)
        dateTime_ = dateTime_.AddYears(amount);
      else
        throw new NotSupportedException
        ("Calendar does not support field " + field);
    }

    private DateTime dateTime_;
    private static DateTime epoch_ = new DateTime(1970, 1, 1);
  }

  public class IlNumber {
    public static string
    ToString(int x, int radix)
    {
      if (radix == 16)
        return x.ToString("x");
      else
        throw new NotSupportedException
          ("IlNumber.ToString does not support radix " + radix);
    }
  }

  public class StringUtil {
    public static int 
    IndexOf(string str, string value, int startIndex) { return str.IndexOf(value, startIndex); }

    public static byte[]
    GetBytes(string str, string encoding)
    {
      if (encoding == "UTF-8")
        return Encoding.UTF8.GetBytes(str);
      else
        throw new NotSupportedException
          ("StringUtil.GetBytes does not support encoding " + encoding);
    }

    public static string
    NewString(byte[] array, string encoding)
    {
      if (encoding == "UTF-8")
        return Encoding.UTF8.GetString(array);
      else
        throw new NotSupportedException
          ("StringUtil.GetBytes does not support encoding " + encoding);
    }

    public static string
    NewString(char[] array) { return new string(array); }

    public static string
    ReplaceFirst(string str, string regex, string replacement)
    {
      var match = new Regex(regex).Match(str);
      if (!match.Success)
        return str;

      int start = match.Groups[0].Index;
      int end = start + match.Groups[0].Length;
      return str.Substring(0, start) + replacement + str.Substring(end);
    }
  }
}

namespace ILOG.J2CsMapping.Util.Logging {
  public enum Level { SEVERE, WARNING, INFO, CONFIG, FINE, FINER, FINEST }

  public class Logger {
    public Logger(string className) {
      className_ = className;
    }

    public static Logger 
    getLogger(string className) { return new Logger(className); }

    public void log(Level level, string message, object arg)
    {
      // TODO: Implement.
    }

    public void log(Level level, string message) { log(level, message, null); }

    private string className_;
  }
}
