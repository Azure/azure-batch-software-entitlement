//
// This is a pruned/limited version of the JSON support that implements
// required functionality, yet will compile with Visual Studio 2012 (VC11) and
// Visual Studio 2013 (VC12).  It is not recommended to use this outside of
// this project.
//

/*
    __ _____ _____ _____
 __|  |   __|     |   | |  JSON for Modern C++
|  |  |__   |  |  | | | |  version 2.1.1
|_____|_____|_____|_|___|  https://github.com/nlohmann/json

Licensed under the MIT License <http://opensource.org/licenses/MIT>.
Copyright (c) 2013-2017 Niels Lohmann <http://nlohmann.me>.

Permission is hereby  granted, free of charge, to any  person obtaining a copy
of this software and associated  documentation files (the "Software"), to deal
in the Software  without restriction, including without  limitation the rights
to  use, copy,  modify, merge,  publish, distribute,  sublicense, and/or  sell
copies  of  the Software,  and  to  permit persons  to  whom  the Software  is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE  IS PROVIDED "AS  IS", WITHOUT WARRANTY  OF ANY KIND,  EXPRESS OR
IMPLIED,  INCLUDING BUT  NOT  LIMITED TO  THE  WARRANTIES OF  MERCHANTABILITY,
FITNESS FOR  A PARTICULAR PURPOSE AND  NONINFRINGEMENT. IN NO EVENT  SHALL THE
AUTHORS  OR COPYRIGHT  HOLDERS  BE  LIABLE FOR  ANY  CLAIM,  DAMAGES OR  OTHER
LIABILITY, WHETHER IN AN ACTION OF  CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE  OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#ifndef NLOHMANN_JSON_HPP
#define NLOHMANN_JSON_HPP

#include <algorithm> // all_of, copy, fill, find, for_each, none_of, remove, reverse, transform
#include <array> // array
#include <cassert> // assert
#include <ciso646> // and, not, or
#include <clocale> // lconv, localeconv
#include <cmath> // isfinite, labs, ldexp, signbit
#include <cstddef> // nullptr_t, ptrdiff_t, size_t
#include <cstdint> // int64_t, uint64_t
#include <cstdlib> // abort, strtod, strtof, strtold, strtoul, strtoll, strtoull
#include <cstring> // strlen
#include <forward_list> // forward_list
#include <functional> // function, hash, less
#include <iostream> // istream, ostream
#include <iterator> // advance, begin, back_inserter, bidirectional_iterator_tag, distance, end, inserter, iterator, iterator_traits, next, random_access_iterator_tag, reverse_iterator
#include <limits> // numeric_limits
#include <locale> // locale
#include <map> // map
#include <memory> // addressof, allocator, allocator_traits, unique_ptr
#include <numeric> // accumulate
#include <sstream> // stringstream
#include <string> // getline, stoi, string, to_string
#include <type_traits> // add_pointer, conditional, decay, enable_if, false_type, integral_constant, is_arithmetic, is_base_of, is_const, is_constructible, is_convertible, is_default_constructible, is_enum, is_floating_point, is_integral, is_nothrow_move_assignable, is_nothrow_move_constructible, is_pointer, is_reference, is_same, is_scalar, is_signed, remove_const, remove_cv, remove_pointer, remove_reference, true_type, underlying_type
#include <utility> // declval, forward, make_pair, move, pair, swap
#include <vector> // vector

//
// Earlier versions of visual studio don't know of 'noexcept' keyword...
//
#define noexcept _NOEXCEPT

// exclude unsupported compilers
#if defined(__clang__)
    #if (__clang_major__ * 10000 + __clang_minor__ * 100 + __clang_patchlevel__) < 30400
        #error "unsupported Clang version - see https://github.com/nlohmann/json#supported-compilers"
    #endif
#elif defined(__GNUC__)
    #if (__GNUC__ * 10000 + __GNUC_MINOR__ * 100 + __GNUC_PATCHLEVEL__) < 40900
        #error "unsupported GCC version - see https://github.com/nlohmann/json#supported-compilers"
    #endif
#endif

// disable float-equal warnings on GCC/clang
#if defined(__clang__) || defined(__GNUC__) || defined(__GNUG__)
    #pragma GCC diagnostic push
    #pragma GCC diagnostic ignored "-Wfloat-equal"
#endif

// disable documentation warnings on clang
#if defined(__clang__)
    #pragma GCC diagnostic push
    #pragma GCC diagnostic ignored "-Wdocumentation"
#endif

// allow to disable exceptions
#if (defined(__cpp_exceptions) || defined(__EXCEPTIONS) || defined(_CPPUNWIND)) && not defined(JSON_NOEXCEPTION)
    #define JSON_THROW(exception) throw exception
    #define JSON_TRY try
    #define JSON_CATCH(exception) catch(exception)
#else
    #define JSON_THROW(exception) std::abort()
    #define JSON_TRY if(true)
    #define JSON_CATCH(exception) if(false)
#endif

/*!
@brief namespace for Niels Lohmann
@see https://github.com/nlohmann
@since version 1.0.0
*/
namespace nlohmann
{

/*!
@brief unnamed namespace with internal helper functions

This namespace collects some functions that could not be defined inside the
@ref basic_json class.

@since version 2.1.0
*/
namespace detail
{
////////////////
// exceptions //
////////////////

/*!
@brief general exception of the @ref basic_json class

Extension of std::exception objects with a member @a id for exception ids.

@since version 3.0.0
*/
class exception : public std::exception
{
  exception& operator =(const exception& rhs);

  public:
    /// create exception with id an explanatory string
    exception(int id_, const std::string& ename, const std::string& what_arg_)
        : id(id_),
          what_arg("[json.exception." + ename + "." + std::to_string(id_) + "] " + what_arg_)
    {}

    /// returns the explanatory string
    virtual const char* what() const noexcept override
    {
        return what_arg.c_str();
    }

    /// the id of the exception
    const int id;

  private:
    /// the explanatory string
    const std::string what_arg;
};

/*!
@brief exception indicating a parse error

This excpetion is thrown by the library when a parse error occurs. Parse
errors can occur during the deserialization of JSON text as well as when
using JSON Patch.

Member @a byte holds the byte index of the last read character in the input
file.

@note For an input with n bytes, 1 is the index of the first character
      and n+1 is the index of the terminating null byte or the end of
      file. This also holds true when reading a byte vector (CBOR or
      MessagePack).

Exceptions have ids 1xx.

name / id                      | example massage | description
------------------------------ | --------------- | -------------------------
json.exception.parse_error.101 | parse error at 2: unexpected end of input; expected string literal | This error indicates a syntax error while deserializing a JSON text. The error message describes that an unexpected token (character) was encountered, and the member @a byte indicates the error position.
json.exception.parse_error.102 | parse error at 14: missing or wrong low surrogate | JSON uses the `\uxxxx` format to describe Unicode characters. Code points above above 0xFFFF are split into two `\uxxxx` entries ("surrogate pairs"). This error indicates that the surrogate pair is incomplete or contains an invalid code point.
json.exception.parse_error.103 | parse error: code points above 0x10FFFF are invalid | Unicode supports code points up to 0x10FFFF. Code points above 0x10FFFF are invalid.
json.exception.parse_error.104 | parse error: JSON patch must be an array of objects | [RFC 6902](https://tools.ietf.org/html/rfc6902) requires a JSON Patch document to be a JSON document that represents an array of objects.
json.exception.parse_error.105 | parse error: operation must have string member 'op' | An operation of a JSON Patch document must contain exactly one "op" member, whose value indicates the operation to perform. Its value must be one of "add", "remove", "replace", "move", "copy", or "test"; other values are errors.
json.exception.parse_error.106 | parse error: array index '01' must not begin with '0' | An array index in a JSON Pointer ([RFC 6901](https://tools.ietf.org/html/rfc6901)) may be `0` or any number wihtout a leading `0`.
json.exception.parse_error.107 | parse error: JSON pointer must be empty or begin with '/' - was: 'foo' | A JSON Pointer must be a Unicode string containing a sequence of zero or more reference tokens, each prefixed by a `/` character.
json.exception.parse_error.108 | parse error: escape character '~' must be followed with '0' or '1' | In a JSON Pointer, only `~0` and `~1` are valid escape sequences.
json.exception.parse_error.109 | parse error: array index 'one' is not a number | A JSON Pointer array index must be a number.
json.exception.parse_error.110 | parse error at 1: cannot read 2 bytes from vector | When parsing CBOR or MessagePack, the byte vector ends before the complete value has been read.
json.exception.parse_error.111 | parse error: bad input stream | Parsing CBOR or MessagePack from an input stream where the [`badbit` or `failbit`](http://en.cppreference.com/w/cpp/io/ios_base/iostate) is set.
json.exception.parse_error.112 | parse error at 1: error reading CBOR; last byte: 0xf8 | Not all types of CBOR or MessagePack are supported. This exception occurs if an unsupported byte was read.
json.exception.parse_error.113 | parse error at 2: expected a CBOR string; last byte: 0x98 | While parsing a map key, a value that is not a string has been read.

@since version 3.0.0
*/
class parse_error : public exception
{
  public:
    /*!
    @brief create a parse error exception
    @param[in] id_        the id of the exception
    @param[in] byte_      the byte index where the error occured (or 0 if
                          the position cannot be determined)
    @param[in] what_arg_  the explanatory string
    */
    parse_error(int id_, size_t byte_, const std::string& what_arg_)
        : exception(id_, "parse_error", "parse error" +
                    (byte_ != 0 ? (" at " + std::to_string(byte_)) : "") +
                    ": " + what_arg_),
          byte(byte_)
    {}

    /*!
    @brief byte index of the parse error

    The byte index of the last read character in the input file.

    @note For an input with n bytes, 1 is the index of the first character
          and n+1 is the index of the terminating null byte or the end of
          file. This also holds true when reading a byte vector (CBOR or
          MessagePack).
    */
    const size_t byte;
};

/*!
@brief exception indicating errors with iterators

Exceptions have ids 2xx.

name / id                           | example massage | description
----------------------------------- | --------------- | -------------------------
json.exception.invalid_iterator.201 | iterators are not compatible | The iterators passed to constructor @ref basic_json(InputIT first, InputIT last) are not compatible, meaning they do not belong to the same container. Therefore, the range (@a first, @a last) is invalid.
json.exception.invalid_iterator.202 | iterator does not fit current value | In an erase or insert function, the passed iterator @a pos does not belong to the JSON value for which the function was called. It hence does not define a valid position for the deletion/insertion.
json.exception.invalid_iterator.203 | iterators do not fit current value | Either iterator passed to function @ref erase(IteratorType first, IteratorType last) does not belong to the JSON value from which values shall be erased. It hence does not define a valid range to delete values from.
json.exception.invalid_iterator.204 | iterators out of range | When an iterator range for a primitive type (number, boolean, or string) is passed to a constructor or an erase function, this range has to be exactly (@ref begin(), @ref end()), because this is the only way the single stored value is expressed. All other ranges are invalid.
json.exception.invalid_iterator.205 | iterator out of range | When an iterator for a primitive type (number, boolean, or string) is passed to an erase function, the iterator has to be the @ref begin() iterator, because it is the only way to address the stored value. All other iterators are invalid.
json.exception.invalid_iterator.206 | cannot construct with iterators from null | The iterators passed to constructor @ref basic_json(InputIT first, InputIT last) belong to a JSON null value and hence to not define a valid range.
json.exception.invalid_iterator.207 | cannot use key() for non-object iterators | The key() member function can only be used on iterators belonging to a JSON object, because other types do not have a concept of a key.
json.exception.invalid_iterator.208 | cannot use operator[] for object iterators | The operator[] to specify a concrete offset cannot be used on iterators belonging to a JSON object, because JSON objects are unordered.
json.exception.invalid_iterator.209 | cannot use offsets with object iterators | The offset operators (+, -, +=, -=) cannot be used on iterators belonging to a JSON object, because JSON objects are unordered.
json.exception.invalid_iterator.210 | iterators do not fit | The iterator range passed to the insert function are not compatible, meaning they do not belong to the same container. Therefore, the range (@a first, @a last) is invalid.
json.exception.invalid_iterator.211 | passed iterators may not belong to container | The iterator range passed to the insert function must not be a subrange of the container to insert to.
json.exception.invalid_iterator.212 | cannot compare iterators of different containers | When two iterators are compared, they must belong to the same container.
json.exception.invalid_iterator.213 | cannot compare order of object iterators | The order of object iterators cannot be compated, because JSON objects are unordered.
json.exception.invalid_iterator.214 | cannot get value | Cannot get value for iterator: Either the iterator belongs to a null value or it is an iterator to a primitive type (number, boolean, or string), but the iterator is different to @ref begin().

@since version 3.0.0
*/
class invalid_iterator : public exception
{
  public:
    invalid_iterator(int id_, const std::string& what_arg_)
        : exception(id_, "invalid_iterator", what_arg_)
    {}
};

/*!
@brief exception indicating executing a member function with a wrong type

Exceptions have ids 3xx.

name / id                     | example massage | description
----------------------------- | --------------- | -------------------------
json.exception.type_error.301 | cannot create object from initializer list | To create an object from an initializer list, the initializer list must consist only of a list of pairs whose first element is a string. When this constraint is violated, an array is created instead.
json.exception.type_error.302 | type must be object, but is array | During implicit or explicit value conversion, the JSON type must be compatible to the target type. For instance, a JSON string can only be converted into string types, but not into numbers or boolean types.
json.exception.type_error.303 | incompatible ReferenceType for get_ref, actual type is object | To retrieve a reference to a value stored in a @ref basic_json object with @ref get_ref, the type of the reference must match the value type. For instance, for a JSON array, the @a ReferenceType must be @ref array_t&.
json.exception.type_error.304 | cannot use at() with string | The @ref at() member functions can only be executed for certain JSON types.
json.exception.type_error.305 | cannot use operator[] with string | The @ref operator[] member functions can only be executed for certain JSON types.
json.exception.type_error.306 | cannot use value() with string | The @ref value() member functions can only be executed for certain JSON types.
json.exception.type_error.307 | cannot use erase() with string | The @ref erase() member functions can only be executed for certain JSON types.
json.exception.type_error.308 | cannot use push_back() with string | The @ref push_back() and @ref operator+= member functions can only be executed for certain JSON types.
json.exception.type_error.309 | cannot use insert() with | The @ref insert() member functions can only be executed for certain JSON types.
json.exception.type_error.310 | cannot use swap() with number | The @ref swap() member functions can only be executed for certain JSON types.
json.exception.type_error.311 | cannot use emplace_back() with string | The @ref emplace_back() member function can only be executed for certain JSON types.
json.exception.type_error.313 | invalid value to unflatten | The @ref unflatten function converts an object whose keys are JSON Pointers back into an arbitrary nested JSON value. The JSON Pointers must not overlap, because then the resulting value would not be well defined.
json.exception.type_error.314 | only objects can be unflattened | The @ref unflatten function only works for an object whose keys are JSON Pointers.
json.exception.type_error.315 | values in object must be primitive | The @ref unflatten function only works for an object whose keys are JSON Pointers and whose values are primitive.

@since version 3.0.0
*/
class type_error : public exception
{
  public:
    type_error(int id_, const std::string& what_arg_)
        : exception(id_, "type_error", what_arg_)
    {}
};

/*!
@brief exception indicating access out of the defined range

Exceptions have ids 4xx.

name / id                       | example massage | description
------------------------------- | --------------- | -------------------------
json.exception.out_of_range.401 | array index 3 is out of range | The provided array index @a i is larger than @a size-1.
json.exception.out_of_range.402 | array index '-' (3) is out of range | The special array index `-` in a JSON Pointer never describes a valid element of the array, but the index past the end. That is, it can only be used to add elements at this position, but not to read it.
json.exception.out_of_range.403 | key 'foo' not found | The provided key was not found in the JSON object.
json.exception.out_of_range.404 | unresolved reference token 'foo' | A reference token in a JSON Pointer could not be resolved.
json.exception.out_of_range.405 | JSON pointer has no parent | The JSON Patch operations 'remove' and 'add' can not be applied to the root element of the JSON value.
json.exception.out_of_range.406 | number overflow parsing '10E1000' | A parsed number could not be stored as without changing it to NaN or INF.

@since version 3.0.0
*/
class out_of_range : public exception
{
  public:
    out_of_range(int id_, const std::string& what_arg_)
        : exception(id_, "out_of_range", what_arg_)
    {}
};

/*!
@brief exception indicating other errors

Exceptions have ids 5xx.

name / id                      | example massage | description
------------------------------ | --------------- | -------------------------
json.exception.other_error.501 | unsuccessful: {"op":"test","path":"/baz", "value":"bar"} | A JSON Patch operation 'test' failed. The unsuccessful operation is also printed.

@since version 3.0.0
*/
class other_error : public exception
{
  public:
    other_error(int id_, const std::string& what_arg_)
        : exception(id_, "other_error", what_arg_)
    {}
};



///////////////////////////
// JSON type enumeration //
///////////////////////////

/*!
@brief the JSON type enumeration

This enumeration collects the different JSON types. It is internally used to
distinguish the stored values, and the functions @ref basic_json::is_null(),
@ref basic_json::is_object(), @ref basic_json::is_array(),
@ref basic_json::is_string(), @ref basic_json::is_boolean(),
@ref basic_json::is_number() (with @ref basic_json::is_number_integer(),
@ref basic_json::is_number_unsigned(), and @ref basic_json::is_number_float()),
@ref basic_json::is_discarded(), @ref basic_json::is_primitive(), and
@ref basic_json::is_structured() rely on it.

@note There are three enumeration entries (number_integer, number_unsigned, and
number_float), because the library distinguishes these three types for numbers:
@ref basic_json::number_unsigned_t is used for unsigned integers,
@ref basic_json::number_integer_t is used for signed integers, and
@ref basic_json::number_float_t is used for floating-point numbers or to
approximate integers which do not fit in the limits of their respective type.

@sa @ref basic_json::basic_json(const value_t value_type) -- create a JSON
value with the default value for a given type

@since version 1.0.0
*/
enum class value_t : uint8_t
{
    null,            ///< null value
    object,          ///< object (unordered set of name/value pairs)
    string,          ///< string value
    discarded        ///< discarded by the the parser callback function
};

/*!
@brief comparison operator for JSON types

Returns an ordering that is similar to Python:
- order: null < boolean < number < object < array < string
- furthermore, each type is not smaller than itself

@since version 1.0.0
*/
inline bool operator<(const value_t lhs, const value_t rhs) noexcept
{
    static const std::array<uint8_t, 8> order = {{
            0, // null
            3, // object
            5, // string
        }
    };

    // discarded values are not comparable
    if (lhs == value_t::discarded or rhs == value_t::discarded)
    {
        return false;
    }

    return order[static_cast<std::size_t>(lhs)] <
           order[static_cast<std::size_t>(rhs)];
}


/////////////
// helpers //
/////////////

// dispatch utility (taken from ranges-v3)
template<unsigned N> struct priority_tag : priority_tag < N - 1 > {};
template<> struct priority_tag<0> {};


//////////////////
// constructors //
//////////////////

template<value_t> struct external_constructor;

template<>
struct external_constructor<value_t::string>
{
    template<typename BasicJsonType>
    static void construct(BasicJsonType& j, const typename BasicJsonType::string_t& s)
    {
        j.m_type = value_t::string;
        j.m_value = s;
        j.assert_invariant();
    }
};

template<>
struct external_constructor<value_t::object>
{
    template<typename BasicJsonType>
    static void construct(BasicJsonType& j, const typename BasicJsonType::object_t& obj)
    {
        j.m_type = value_t::object;
        j.m_value = obj;
        j.assert_invariant();
    }
};


/////////////
// to_json //
/////////////

template<typename BasicJsonType>
void to_json(BasicJsonType& j, const typename BasicJsonType::string_t& s)
{
    external_constructor<value_t::string>::construct(j, s);
}

///////////////
// from_json //
///////////////

template<typename BasicJsonType>
void from_json(const BasicJsonType& j, typename BasicJsonType::string_t& s)
{
    if (not j.is_string())
    {
        JSON_THROW(type_error(302, "type must be string, but is " + j.type_name()));
    }
    s = *j.template get_ptr<const typename BasicJsonType::string_t*>();
}

struct to_json_fn
{
  private:
    template<typename BasicJsonType, typename T>
    auto call(BasicJsonType& j, T&& val, priority_tag<1>) const
    -> decltype(to_json(j, std::forward<T>(val)), void())
    {
        return to_json(j, std::forward<T>(val));
    }

    template<typename BasicJsonType, typename T>
    void call(BasicJsonType&, T&&, priority_tag<0>) const noexcept
    {
        static_assert(sizeof(BasicJsonType) == 0,
                      "could not find to_json() method in T's namespace");
    }

  public:
    template<typename BasicJsonType, typename T>
    void operator()(BasicJsonType& j, T&& val) const
    {
        return call(j, std::forward<T>(val), priority_tag<1>());
    }
};

struct from_json_fn
{
  private:
    template<typename BasicJsonType, typename T>
    auto call(const BasicJsonType& j, T& val, priority_tag<1>) const
    -> decltype(from_json(j, val), void())
    {
        return from_json(j, val);
    }

    template<typename BasicJsonType, typename T>
    void call(const BasicJsonType&, T&, priority_tag<0>) const noexcept
    {
        static_assert(sizeof(BasicJsonType) == 0,
                      "could not find from_json() method in T's namespace");
    }

  public:
    template<typename BasicJsonType, typename T>
    void operator()(const BasicJsonType& j, T& val) const
    {
        return call(j, val, priority_tag<1>());
    }
};

// taken from ranges-v3
template<typename T>
struct static_const
{
    static const T value;
};

template<typename T>
const T static_const<T>::value;
} // namespace detail


/// namespace to hold default `to_json` / `from_json` functions
namespace
{
const auto& to_json = detail::static_const<detail::to_json_fn>::value;
const auto& from_json = detail::static_const<detail::from_json_fn>::value;
}


/*!
@brief default JSONSerializer template argument

This serializer ignores the template arguments and uses ADL
([argument-dependent lookup](http://en.cppreference.com/w/cpp/language/adl))
for serialization.
*/
template<typename = void, typename = void>
struct adl_serializer
{
    /*!
    @brief convert a JSON value to any value type

    This function is usually called by the `get()` function of the
    @ref basic_json class (either explicit or via conversion operators).

    @param[in] j         JSON value to read from
    @param[in,out] val  value to write to
    */
    template<typename BasicJsonType, typename ValueType>
    static void from_json(BasicJsonType&& j, ValueType& val)
    {
        ::nlohmann::from_json(std::forward<BasicJsonType>(j), val);
    }

    /*!
    @brief convert any value type to a JSON value

    This function is usually called by the constructors of the @ref basic_json
    class.

    @param[in,out] j  JSON value to write to
    @param[in] val     value to read from
    */
    template<typename BasicJsonType, typename ValueType>
    static void to_json(BasicJsonType& j, ValueType&& val)
    {
        ::nlohmann::to_json(j, std::forward<ValueType>(val));
    }
};


/*!
@brief a class to store JSON values

@requirement The class satisfies the following concept requirements:
- Basic
 - [DefaultConstructible](http://en.cppreference.com/w/cpp/concept/DefaultConstructible):
   JSON values can be default constructed. The result will be a JSON null
   value.
 - [MoveConstructible](http://en.cppreference.com/w/cpp/concept/MoveConstructible):
   A JSON value can be constructed from an rvalue argument.
 - [CopyConstructible](http://en.cppreference.com/w/cpp/concept/CopyConstructible):
   A JSON value can be copy-constructed from an lvalue expression.
 - [MoveAssignable](http://en.cppreference.com/w/cpp/concept/MoveAssignable):
   A JSON value van be assigned from an rvalue argument.
 - [CopyAssignable](http://en.cppreference.com/w/cpp/concept/CopyAssignable):
   A JSON value can be copy-assigned from an lvalue expression.
 - [Destructible](http://en.cppreference.com/w/cpp/concept/Destructible):
   JSON values can be destructed.
- Layout
 - [StandardLayoutType](http://en.cppreference.com/w/cpp/concept/StandardLayoutType):
   JSON values have
   [standard layout](http://en.cppreference.com/w/cpp/language/data_members#Standard_layout):
   All non-static data members are private and standard layout types, the
   class has no virtual functions or (virtual) base classes.
- Library-wide
 - [EqualityComparable](http://en.cppreference.com/w/cpp/concept/EqualityComparable):
   JSON values can be compared with `==`, see @ref
   operator==(const_reference,const_reference).
 - [LessThanComparable](http://en.cppreference.com/w/cpp/concept/LessThanComparable):
   JSON values can be compared with `<`, see @ref
   operator<(const_reference,const_reference).
 - [Swappable](http://en.cppreference.com/w/cpp/concept/Swappable):
   Any JSON lvalue or rvalue of can be swapped with any lvalue or rvalue of
   other compatible types, using unqualified function call @ref swap().
 - [NullablePointer](http://en.cppreference.com/w/cpp/concept/NullablePointer):
   JSON values can be compared against `std::nullptr_t` objects which are used
   to model the `null` value.
- Container
 - [Container](http://en.cppreference.com/w/cpp/concept/Container):
   JSON values can be used like STL containers and provide iterator access.
 - [ReversibleContainer](http://en.cppreference.com/w/cpp/concept/ReversibleContainer);
   JSON values can be used like STL containers and provide reverse iterator
   access.

@invariant The member variables @a m_value and @a m_type have the following
relationship:
- If `m_type == value_t::object`, then `m_value.object != nullptr`.
- If `m_type == value_t::string`, then `m_value.string != nullptr`.
The invariants are checked by member function assert_invariant().

@see [RFC 7159: The JavaScript Object Notation (JSON) Data Interchange
Format](http://rfc7159.net/rfc7159)

@since version 1.0.0

@nosubgrouping
*/
class basic_json
{
  private:
    template<detail::value_t> friend struct detail::external_constructor;

  public:
    // forward declarations
    typedef detail::value_t value_t;
    template<typename U> class iter_impl;
    template<typename Base> class json_reverse_iterator;
    class json_pointer;


    ////////////////
    // exceptions //
    ////////////////

    /// @name exceptions
    /// Classes to implement user-defined exceptions.
    /// @{

    /// @copydoc detail::exception
    typedef detail::exception exception;
    /// @copydoc detail::parse_error
    typedef detail::parse_error parse_error;
    /// @copydoc detail::invalid_iterator
    typedef detail::invalid_iterator invalid_iterator;
    /// @copydoc detail::type_error
    typedef detail::type_error type_error;
    /// @copydoc detail::out_of_range
    typedef detail::out_of_range out_of_range;
    /// @copydoc detail::other_error
    typedef detail::other_error other_error;

    /// @}


    /////////////////////
    // container types //
    /////////////////////

    /// @name container types
    /// The canonic container types to use @ref basic_json like any other STL
    /// container.
    /// @{

    /// the type of elements in a basic_json container
    typedef basic_json value_type;

    /// the type of an element reference
    typedef value_type& reference;
    /// the type of an element const reference
    typedef const value_type& const_reference;

    /// a type to represent container sizes
    typedef std::size_t size_type;

    /// the allocator type
    typedef std::allocator<basic_json> allocator_type;

    /// @}

    ///////////////////////////
    // JSON value data types //
    ///////////////////////////

    /// @name JSON value data types
    /// The data types to store a JSON value. These types are derived from
    /// the template arguments passed to class @ref basic_json.
    /// @{

    /*!
    @brief a type for an object

    [RFC 7159](http://rfc7159.net/rfc7159) describes JSON objects as follows:
    > An object is an unordered collection of zero or more name/value pairs,
    > where a name is a string and a value is a string, number, boolean, null,
    > object, or array.

    To store objects in C++, a type is defined by the template parameters
    described below.

    @tparam ObjectType  the container to store objects (e.g., `std::map` or
    `std::unordered_map`)
    @tparam StringType the type of the keys or names (e.g., `std::string`).
    The comparison function `std::less<StringType>` is used to order elements
    inside the container.
    @tparam AllocatorType the allocator to use for objects (e.g.,
    `std::allocator`)

    #### Default type

    With the default values for @a ObjectType (`std::map`), @a StringType
    (`std::string`), and @a AllocatorType (`std::allocator`), the default
    value for @a object_t is:

    @code {.cpp}
    std::map<
      std::string, // key_type
      basic_json, // value_type
      std::less<std::string>, // key_compare
      std::allocator<std::pair<const std::string, basic_json>> // allocator_type
    >
    @endcode

    #### Behavior

    The choice of @a object_t influences the behavior of the JSON class. With
    the default type, objects have the following behavior:

    - When all names are unique, objects will be interoperable in the sense
      that all software implementations receiving that object will agree on
      the name-value mappings.
    - When the names within an object are not unique, later stored name/value
      pairs overwrite previously stored name/value pairs, leaving the used
      names unique. For instance, `{"key": 1}` and `{"key": 2, "key": 1}` will
      be treated as equal and both stored as `{"key": 1}`.
    - Internally, name/value pairs are stored in lexicographical order of the
      names. Objects will also be serialized (see @ref dump) in this order.
      For instance, `{"b": 1, "a": 2}` and `{"a": 2, "b": 1}` will be stored
      and serialized as `{"a": 2, "b": 1}`.
    - When comparing objects, the order of the name/value pairs is irrelevant.
      This makes objects interoperable in the sense that they will not be
      affected by these differences. For instance, `{"b": 1, "a": 2}` and
      `{"a": 2, "b": 1}` will be treated as equal.

    #### Limits

    [RFC 7159](http://rfc7159.net/rfc7159) specifies:
    > An implementation may set limits on the maximum depth of nesting.

    In this class, the object's limit of nesting is not constraint explicitly.
    However, a maximum depth of nesting may be introduced by the compiler or
    runtime environment. A theoretical limit can be queried by calling the
    @ref max_size function of a JSON object.

    #### Storage

    Objects are stored as pointers in a @ref basic_json type. That is, for any
    access to object values, a pointer of type `object_t*` must be
    dereferenced.

    @sa @ref array_t -- type for an array value

    @since version 1.0.0

    @note The order name/value pairs are added to the object is *not*
    preserved by the library. Therefore, iterating an object may return
    name/value pairs in a different order than they were originally stored. In
    fact, keys will be traversed in alphabetical order as `std::map` with
    `std::less` is used by default. Please note this behavior conforms to [RFC
    7159](http://rfc7159.net/rfc7159), because any order implements the
    specified "unordered" nature of JSON objects.
    */
    typedef std::map<std::string, basic_json> object_t;

    /*!
    @brief a type for a string

    [RFC 7159](http://rfc7159.net/rfc7159) describes JSON strings as follows:
    > A string is a sequence of zero or more Unicode characters.

    To store objects in C++, a type is defined by the template parameter
    described below. Unicode values are split by the JSON class into
    byte-sized characters during deserialization.

    @tparam StringType  the container to store strings (e.g., `std::string`).
    Note this container is used for keys/names in objects, see @ref object_t.

    #### Default type

    With the default values for @a StringType (`std::string`), the default
    value for @a string_t is:

    @code {.cpp}
    std::string
    @endcode

    #### Encoding

    Strings are stored in UTF-8 encoding. Therefore, functions like
    `std::string::size()` or `std::string::length()` return the number of
    bytes in the string rather than the number of characters or glyphs.

    #### String comparison

    [RFC 7159](http://rfc7159.net/rfc7159) states:
    > Software implementations are typically required to test names of object
    > members for equality. Implementations that transform the textual
    > representation into sequences of Unicode code units and then perform the
    > comparison numerically, code unit by code unit, are interoperable in the
    > sense that implementations will agree in all cases on equality or
    > inequality of two strings. For example, implementations that compare
    > strings with escaped characters unconverted may incorrectly find that
    > `"a\\b"` and `"a\u005Cb"` are not equal.

    This implementation is interoperable as it does compare strings code unit
    by code unit.

    #### Storage

    String values are stored as pointers in a @ref basic_json type. That is,
    for any access to string values, a pointer of type `string_t*` must be
    dereferenced.

    @since version 1.0.0
    */
    typedef std::string string_t;

    /// @}

    ////////////////////////
    // JSON value storage //
    ////////////////////////

    /*!
    @brief a JSON value

    The actual storage for a JSON value of the @ref basic_json class. This
    union combines the different storage types for the JSON value types
    defined in @ref value_t.

    JSON type | value_t type    | used type
    --------- | --------------- | ------------------------
    object    | object          | pointer to @ref object_t
    array     | array           | pointer to @ref array_t
    string    | string          | pointer to @ref string_t
    boolean   | boolean         | @ref boolean_t
    number    | number_integer  | @ref number_integer_t
    number    | number_unsigned | @ref number_unsigned_t
    number    | number_float    | @ref number_float_t
    null      | null            | *no value is stored*

    @note Variable-length types (objects, arrays, and strings) are stored as
    pointers. The size of the union should not exceed 64 bits if the default
    value types are used.

    @since version 1.0.0
    */
    union json_value
    {
        /// object (stored with pointer to save storage)
        object_t* object;
        /// string (stored with pointer to save storage)
        string_t* string;

        /// default constructor (for null values)
        json_value() noexcept : object(nullptr) {};
        /// constructor for empty values of a given type
        json_value(value_t t)
        {
            switch (t)
            {
                case value_t::object:
                {
                    object =  new object_t();
                    break;
                }

                case value_t::string:
                {
                    string = new string_t("");
                    break;
                }

                case value_t::null:
                {
                    break;
                }

                default:
                {
                    if (t == value_t::null)
                    {
                        JSON_THROW(other_error(500, "961c151d2e87f2686a955a9be24d316f1362bf21 2.1.1")); // LCOV_EXCL_LINE
                    }
                    break;
                }
            }
        }

        /// constructor for strings
        json_value(const string_t& value)
        {
            string = new string_t(value);
        }

        /// constructor for objects
        json_value(const object_t& value)
        {
            object = new object_t(value);
        }
    };

    /*!
    @brief checks the class invariants

    This function asserts the class invariants. It needs to be called at the
    end of every constructor to make sure that created objects respect the
    invariant. Furthermore, it has to be called each time the type of a JSON
    value is changed, because the invariant expresses a relationship between
    @a m_type and @a m_value.
    */
    void assert_invariant() const
    {
        assert(m_type != value_t::object or m_value.object != nullptr);
        assert(m_type != value_t::string or m_value.string != nullptr);
    }

  public:
    //////////////////////////
    // JSON parser callback //
    //////////////////////////

    /*!
    @brief JSON callback events

    This enumeration lists the parser events that can trigger calling a
    callback function of type @ref parser_callback_t during parsing.

    @image html callback_events.png "Example when certain parse events are triggered"

    @since version 1.0.0
    */
    enum class parse_event_t : uint8_t
    {
        /// the parser read `{` and started to process a JSON object
        object_start,
        /// the parser read `}` and finished processing a JSON object
        object_end,
        /// the parser read `[` and started to process a JSON array
        array_start,
        /// the parser read `]` and finished processing a JSON array
        array_end,
        /// the parser read a key of a value in an object
        key,
        /// the parser finished reading a JSON value
        value
    };

    /*!
    @brief per-element parser callback type

    With a parser callback function, the result of parsing a JSON text can be
    influenced. When passed to @ref parse(std::istream&, const
    parser_callback_t) or @ref parse(const CharT, const parser_callback_t),
    it is called on certain events (passed as @ref parse_event_t via parameter
    @a event) with a set recursion depth @a depth and context JSON value
    @a parsed. The return value of the callback function is a boolean
    indicating whether the element that emitted the callback shall be kept or
    not.

    We distinguish six scenarios (determined by the event type) in which the
    callback function can be called. The following table describes the values
    of the parameters @a depth, @a event, and @a parsed.

    parameter @a event | description | parameter @a depth | parameter @a parsed
    ------------------ | ----------- | ------------------ | -------------------
    parse_event_t::object_start | the parser read `{` and started to process a JSON object | depth of the parent of the JSON object | a JSON value with type discarded
    parse_event_t::key | the parser read a key of a value in an object | depth of the currently parsed JSON object | a JSON string containing the key
    parse_event_t::object_end | the parser read `}` and finished processing a JSON object | depth of the parent of the JSON object | the parsed JSON object
    parse_event_t::array_start | the parser read `[` and started to process a JSON array | depth of the parent of the JSON array | a JSON value with type discarded
    parse_event_t::array_end | the parser read `]` and finished processing a JSON array | depth of the parent of the JSON array | the parsed JSON array
    parse_event_t::value | the parser finished reading a JSON value | depth of the value | the parsed JSON value

    @image html callback_events.png "Example when certain parse events are triggered"

    Discarding a value (i.e., returning `false`) has different effects
    depending on the context in which function was called:

    - Discarded values in structured types are skipped. That is, the parser
      will behave as if the discarded value was never read.
    - In case a value outside a structured type is skipped, it is replaced
      with `null`. This case happens if the top-level element is skipped.

    @param[in] depth  the depth of the recursion during parsing

    @param[in] event  an event of type parse_event_t indicating the context in
    the callback function has been called

    @param[in,out] parsed  the current intermediate parse result; note that
    writing to this value has no effect for parse_event_t::key events

    @return Whether the JSON value which called the function during parsing
    should be kept (`true`) or not (`false`). In the latter case, it is either
    skipped completely or replaced by an empty discarded object.

    @sa @ref parse(std::istream&, parser_callback_t) or
    @ref parse(const CharT, const parser_callback_t) for examples

    @since version 1.0.0
    */
    typedef std::function<bool(int depth,
                              parse_event_t event,
                              basic_json& parsed)> parser_callback_t;


    //////////////////
    // constructors //
    //////////////////

    /// @name constructors and destructors
    /// Constructors of class @ref basic_json, copy/move constructor, copy
    /// assignment, static functions creating objects, and the destructor.
    /// @{

    /*!
    @brief create an empty value with a given type

    Create an empty JSON value with a given type. The value will be default
    initialized with an empty value which depends on the type:

    Value type  | initial value
    ----------- | -------------
    null        | `null`
    boolean     | `false`
    string      | `""`
    number      | `0`
    object      | `{}`
    array       | `[]`

    @param[in] value_type  the type of the value to create

    @complexity Constant.

    @liveexample{The following code shows the constructor for different @ref
    value_t values,basic_json__value_t}

    @since version 1.0.0
    */
    basic_json(const value_t value_type)
        : m_type(value_type), m_value(value_type)
    {
        assert_invariant();
    }

    /*!
    @brief create a null object

    Create a `null` JSON value. It either takes a null pointer as parameter
    (explicitly creating `null`) or no parameter (implicitly creating `null`).
    The passed null pointer itself is not read -- it is only used to choose
    the right constructor.

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this constructor never throws
    exceptions.

    @liveexample{The following code shows the constructor with and without a
    null pointer parameter.,basic_json__nullptr_t}

    @since version 1.0.0
    */
    basic_json(std::nullptr_t = nullptr) noexcept
        : m_type(value_t::null), m_value(value_t::null)
    {
        assert_invariant();
    }

    /*!
    @brief create a JSON value

    This is a "catch all" constructor for all compatible JSON types; that is,
    types for which a `to_json()` method exsits. The constructor forwards the
    parameter @a val to that method (to `json_serializer<U>::to_json` method
    with `U = uncvref_t<CompatibleType>`, to be exact).

    Template type @a CompatibleType includes, but is not limited to, the
    following types:
    - **arrays**: @ref array_t and all kinds of compatible containers such as
      `std::vector`, `std::deque`, `std::list`, `std::forward_list`,
      `std::array`, `std::set`, `std::unordered_set`, `std::multiset`, and
      `unordered_multiset` with a `value_type` from which a @ref basic_json
      value can be constructed.
    - **objects**: @ref object_t and all kinds of compatible associative
      containers such as `std::map`, `std::unordered_map`, `std::multimap`,
      and `std::unordered_multimap` with a `key_type` compatible to
      @ref string_t and a `value_type` from which a @ref basic_json value can
      be constructed.
    - **strings**: @ref string_t, string literals, and all compatible string
      containers can be used.
    - **numbers**: @ref number_integer_t, @ref number_unsigned_t,
      @ref number_float_t, and all convertible number types such as `int`,
      `size_t`, `int64_t`, `float` or `double` can be used.
    - **boolean**: @ref boolean_t / `bool` can be used.

    See the examples below.

    @tparam CompatibleType a type such that:
    - @a CompatibleType is not derived from `std::istream`,
    - @a CompatibleType is not @ref basic_json (to avoid hijacking copy/move
         constructors),
    - @a CompatibleType is not a @ref basic_json nested type (e.g.,
         @ref json_pointer, @ref iterator, etc ...)
    - @ref @ref json_serializer<U> has a
         `to_json(basic_json_t&, CompatibleType&&)` method

    @tparam U = `uncvref_t<CompatibleType>`

    @param[in] val the value to be forwarded

    @complexity Usually linear in the size of the passed @a val, also
                depending on the implementation of the called `to_json()`
                method.

    @throw what `json_serializer<U>::to_json()` throws

    @liveexample{The following code shows the constructor with several
    compatible types.,basic_json__CompatibleType}

    @since version 2.1.0
    */
    basic_json(const std::string & val)
    {
        adl_serializer<std::string>::to_json(*this, val);
        assert_invariant();
    }


    ///////////////////////////////////////
    // other constructors and destructor //
    ///////////////////////////////////////

    /*!
    @brief copy constructor

    Creates a copy of a given JSON value.

    @param[in] other  the JSON value to copy

    @complexity Linear in the size of @a other.

    @requirement This function helps `basic_json` satisfying the
    [Container](http://en.cppreference.com/w/cpp/concept/Container)
    requirements:
    - The complexity is linear.
    - As postcondition, it holds: `other == basic_json(other)`.

    @liveexample{The following code shows an example for the copy
    constructor.,basic_json__basic_json}

    @since version 1.0.0
    */
    basic_json(const basic_json& other)
        : m_type(other.m_type)
    {
        // check of passed value is valid
        other.assert_invariant();

        switch (m_type)
        {
            case value_t::object:
            {
                m_value = *other.m_value.object;
                break;
            }

            case value_t::string:
            {
                m_value = *other.m_value.string;
                break;
            }

            default:
            {
                break;
            }
        }

        assert_invariant();
    }

    /*!
    @brief move constructor

    Move constructor. Constructs a JSON value with the contents of the given
    value @a other using move semantics. It "steals" the resources from @a
    other and leaves it as JSON null value.

    @param[in,out] other  value to move to this object

    @post @a other is a JSON null value

    @complexity Constant.

    @liveexample{The code below shows the move constructor explicitly called
    via std::move.,basic_json__moveconstructor}

    @since version 1.0.0
    */
    basic_json(basic_json&& other) noexcept
        : m_type(std::move(other.m_type)),
          m_value(std::move(other.m_value))
    {
        // check that passed value is valid
        other.assert_invariant();

        // invalidate payload
        other.m_type = value_t::null;
        other.m_value.object = nullptr;

        assert_invariant();
    }

    /*!
    @brief copy assignment

    Copy assignment operator. Copies a JSON value via the "copy and swap"
    strategy: It is expressed in terms of the copy constructor, destructor,
    and the swap() member function.

    @param[in] other  value to copy from

    @complexity Linear.

    @requirement This function helps `basic_json` satisfying the
    [Container](http://en.cppreference.com/w/cpp/concept/Container)
    requirements:
    - The complexity is linear.

    @liveexample{The code below shows and example for the copy assignment. It
    creates a copy of value `a` which is then swapped with `b`. Finally\, the
    copy of `a` (which is the null value after the swap) is
    destroyed.,basic_json__copyassignment}

    @since version 1.0.0
    */
    reference& operator=(basic_json other) noexcept
    {
        // check that passed value is valid
        other.assert_invariant();

        using std::swap;
        swap(m_type, other.m_type);
        swap(m_value, other.m_value);

        assert_invariant();
        return *this;
    }

    /*!
    @brief destructor

    Destroys the JSON value and frees all allocated memory.

    @complexity Linear.

    @requirement This function helps `basic_json` satisfying the
    [Container](http://en.cppreference.com/w/cpp/concept/Container)
    requirements:
    - The complexity is linear.
    - All stored elements are destroyed and all memory is freed.

    @since version 1.0.0
    */
    ~basic_json()
    {
        assert_invariant();

        switch (m_type)
        {
            case value_t::object:
            {
                delete m_value.object;
                break;
            }

            case value_t::string:
            {
                delete m_value.string;
                break;
            }

            default:
            {
                // all other types need no specific destructor
                break;
            }
        }
    }

    /// @}

  public:
    ///////////////////////
    // object inspection //
    ///////////////////////

    /// @name object inspection
    /// Functions to inspect the type of a JSON value.
    /// @{

    /*!
    @brief serialization

    Serialization function for JSON values. The function tries to mimic
    Python's `json.dumps()` function, and currently supports its @a indent
    parameter.

    @param[in] indent If indent is nonnegative, then array elements and object
    members will be pretty-printed with that indent level. An indent level of
    `0` will only insert newlines. `-1` (the default) selects the most compact
    representation.

    @return string containing the serialization of the JSON value

    @complexity Linear.

    @liveexample{The following example shows the effect of different @a indent
    parameters to the result of the serialization.,dump}

    @see https://docs.python.org/2/library/json.html#json.dump

    @since version 1.0.0
    */
    string_t dump(const int indent = -1) const
    {
        std::stringstream ss;
        serializer s(ss);

        if (indent >= 0)
        {
            s.dump(*this, true, static_cast<unsigned int>(indent));
        }
        else
        {
            s.dump(*this, false, 0);
        }

        return ss.str();
    }

    /*!
    @brief return the type of the JSON value (explicit)

    Return the type of the JSON value as a value from the @ref value_t
    enumeration.

    @return the type of the JSON value

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this member function never throws
    exceptions.

    @liveexample{The following code exemplifies `type()` for all JSON
    types.,type}

    @since version 1.0.0
    */
    const value_t type() const noexcept
    {
        return m_type;
    }

    /*!
    @brief return whether type is primitive

    This function returns true iff the JSON type is primitive (string, number,
    boolean, or null).

    @return `true` if type is primitive (string, number, boolean, or null),
    `false` otherwise.

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this member function never throws
    exceptions.

    @liveexample{The following code exemplifies `is_primitive()` for all JSON
    types.,is_primitive}

    @sa @ref is_structured() -- returns whether JSON value is structured
    @sa @ref is_null() -- returns whether JSON value is `null`
    @sa @ref is_string() -- returns whether JSON value is a string
    @sa @ref is_boolean() -- returns whether JSON value is a boolean
    @sa @ref is_number() -- returns whether JSON value is a number

    @since version 1.0.0
    */
    const bool is_primitive() const noexcept
    {
        return is_null() or is_string();
    }

    /*!
    @brief return whether value is null

    This function returns true iff the JSON value is null.

    @return `true` if type is null, `false` otherwise.

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this member function never throws
    exceptions.

    @liveexample{The following code exemplifies `is_null()` for all JSON
    types.,is_null}

    @since version 1.0.0
    */
    const bool is_null() const noexcept
    {
        return m_type == value_t::null;
    }

    /*!
    @brief return whether value is an object

    This function returns true iff the JSON value is an object.

    @return `true` if type is object, `false` otherwise.

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this member function never throws
    exceptions.

    @liveexample{The following code exemplifies `is_object()` for all JSON
    types.,is_object}

    @since version 1.0.0
    */
    const bool is_object() const noexcept
    {
        return m_type == value_t::object;
    }

    /*!
    @brief return whether value is a string

    This function returns true iff the JSON value is a string.

    @return `true` if type is string, `false` otherwise.

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this member function never throws
    exceptions.

    @liveexample{The following code exemplifies `is_string()` for all JSON
    types.,is_string}

    @since version 1.0.0
    */
    const bool is_string() const noexcept
    {
        return m_type == value_t::string;
    }

    /*!
    @brief return whether value is discarded

    This function returns true iff the JSON value was discarded during parsing
    with a callback function (see @ref parser_callback_t).

    @note This function will always be `false` for JSON values after parsing.
    That is, discarded values can only occur during parsing, but will be
    removed when inside a structured value or replaced by null in other cases.

    @return `true` if type is discarded, `false` otherwise.

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this member function never throws
    exceptions.

    @liveexample{The following code exemplifies `is_discarded()` for all JSON
    types.,is_discarded}

    @since version 1.0.0
    */
    const bool is_discarded() const noexcept
    {
        return m_type == value_t::discarded;
    }

    /*!
    @brief return the type of the JSON value (implicit)

    Implicitly return the type of the JSON value as a value from the @ref
    value_t enumeration.

    @return the type of the JSON value

    @complexity Constant.

    @exceptionsafety No-throw guarantee: this member function never throws
    exceptions.

    @liveexample{The following code exemplifies the @ref value_t operator for
    all JSON types.,operator__value_t}

    @since version 1.0.0
    */
    operator value_t() const noexcept
    {
        return m_type;
    }

    /// @}

  private:
    //////////////////
    // value access //
    //////////////////

    /// get a pointer to the value (object)
    object_t* get_impl_ptr(object_t* /*unused*/) noexcept
    {
        return is_object() ? m_value.object : nullptr;
    }

    /// get a pointer to the value (object)
    const object_t* get_impl_ptr(const object_t* /*unused*/) const noexcept
    {
        return is_object() ? m_value.object : nullptr;
    }

    /// get a pointer to the value (string)
    string_t* get_impl_ptr(string_t* /*unused*/) noexcept
    {
        return is_string() ? m_value.string : nullptr;
    }

    /// get a pointer to the value (string)
    const string_t* get_impl_ptr(const string_t* /*unused*/) const noexcept
    {
        return is_string() ? m_value.string : nullptr;
    }

    /*!
    @brief helper function to implement get_ref()

    This funcion helps to implement get_ref() without code duplication for
    const and non-const overloads

    @tparam ThisType will be deduced as `basic_json` or `const basic_json`

    @throw type_error.303 if ReferenceType does not match underlying value
    type of the current JSON
    */
    template<typename ReferenceType, typename ThisType>
    static ReferenceType get_ref_impl(ThisType& obj)
    {
        // helper type
        using PointerType = typename std::add_pointer<ReferenceType>::type;

        // delegate the call to get_ptr<>()
        auto ptr = obj.template get_ptr<PointerType>();

        if (ptr != nullptr)
        {
            return *ptr;
        }

        JSON_THROW(type_error(303, "incompatible ReferenceType for get_ref, actual type is " + obj.type_name()));
    }

  public:
    /// @name value access
    /// Direct access to the stored value of a JSON value.
    /// @{

    /*!
    @brief get a value (explicit)

    Explicit type conversion between the JSON value and a compatible value
    which is [CopyConstructible](http://en.cppreference.com/w/cpp/concept/CopyConstructible)
    and [DefaultConstructible](http://en.cppreference.com/w/cpp/concept/DefaultConstructible).
    The value is converted by calling the @ref json_serializer<ValueType>
    `from_json()` method.

    The function is equivalent to executing
    @code {.cpp}
    ValueType ret;
    JSONSerializer<ValueType>::from_json(*this, ret);
    return ret;
    @endcode

    This overloads is chosen if:
    - @a ValueType is not @ref basic_json,
    - @ref json_serializer<ValueType> has a `from_json()` method of the form
      `void from_json(const @ref basic_json&, ValueType&)`, and
    - @ref json_serializer<ValueType> does not have a `from_json()` method of
      the form `ValueType from_json(const @ref basic_json&)`

    @tparam ValueTypeCV the provided value type
    @tparam ValueType the returned value type

    @return copy of the JSON value, converted to @a ValueType

    @throw what @ref json_serializer<ValueType> `from_json()` method throws

    @liveexample{The example below shows several conversions from JSON values
    to other types. There a few things to note: (1) Floating-point numbers can
    be converted to integers\, (2) A JSON array can be converted to a standard
    `std::vector<short>`\, (3) A JSON object can be converted to C++
    associative containers such as `std::unordered_map<std::string\,
    json>`.,get__ValueType_const}

    @since version 2.1.0
    */
    template <typename ValueType>
    ValueType get() const
    {
        // we cannot static_assert on ValueTypeCV being non-const, because
        // there is support for get<const basic_json>(), which is why we
        // still need the uncvref
        static_assert(not std::is_reference<ValueType>::value,
                      "get() cannot be used with reference types, you might want to use get_ref()");
        static_assert(std::is_default_constructible<ValueType>::value,
                      "types must be DefaultConstructible when used with get()");

        ValueType ret;
        adl_serializer<ValueType>::from_json(*this, ret);
        return ret;
    }

    /*!
    @brief get a pointer value (implicit)
    @copydoc get_ptr()
    */
    template<typename PointerType>
    const PointerType get_ptr() const noexcept
    {
        // get the type of the PointerType (remove pointer and const)
        typedef typename std::remove_const<typename
            std::remove_pointer<typename
            std::remove_const<PointerType>::type>::type>::type pointee_t;
        // make sure the type matches the allowed types
        static_assert(
            std::is_same<object_t, pointee_t>::value
            or std::is_same<string_t, pointee_t>::value
            , "incompatible pointer type");

        // delegate the call to get_impl_ptr<>() const
        return get_impl_ptr(static_cast<const PointerType>(nullptr));
    }

    /*!
    @brief get a value (implicit)

    Implicit type conversion between the JSON value and a compatible value.
    The call is realized by calling @ref get() const.

    @tparam ValueType non-pointer type compatible to the JSON value, for
    instance `int` for JSON integer numbers, `bool` for JSON booleans, or
    `std::vector` types for JSON arrays. The character type of @ref string_t
    as well as an initializer list of this type is excluded to avoid
    ambiguities as these types implicitly convert to `std::string`.

    @return copy of the JSON value, converted to type @a ValueType

    @throw type_error.302 in case passed type @a ValueType is incompatible
    to the JSON value type (e.g., the JSON value is of type boolean, but a
    string is requested); see example below

    @complexity Linear in the size of the JSON value.

    @liveexample{The example below shows several conversions from JSON values
    to other types. There a few things to note: (1) Floating-point numbers can
    be converted to integers\, (2) A JSON array can be converted to a standard
    `std::vector<short>`\, (3) A JSON object can be converted to C++
    associative containers such as `std::unordered_map<std::string\,
    json>`.,operator__ValueType}

    @since version 1.0.0
    */
    operator std::string() const
    {
        return get<std::string>();
    }

    /// @}


    ////////////////////
    // element access //
    ////////////////////

    /// @name element access
    /// Access to the JSON value.
    /// @{

    /*!
    @brief access specified object element with bounds checking

    Returns a reference to the element at with specified key @a key, with
    bounds checking.

    @param[in] key  key of the element to access

    @return reference to the element at key @a key

    @throw type_error.304 if the JSON value is not an object; in this case,
    calling `at` with a key makes no sense. See example below.
    @throw out_of_range.403 if the key @a key is is not stored in the object;
    that is, `find(key) == end()`. See example below.

    @exceptionsafety Strong guarantee: if an exception is thrown, there are no
    changes in the JSON value.

    @complexity Logarithmic in the size of the container.

    @sa @ref operator[](const typename object_t::key_type&) for unchecked
    access by reference
    @sa @ref value() for access by value with a default value

    @since version 1.0.0

    @liveexample{The example below shows how object elements can be read and
    written using `at()`. It also demonstrates the different exceptions that
    can be thrown.,at__object_t_key_type}
    */
    reference at(const object_t::key_type& key)
    {
        // at only works for objects
        if (is_object())
        {
            JSON_TRY
            {
                return m_value.object->at(key);
            }
            JSON_CATCH (std::out_of_range&)
            {
                // create better exception explanation
                JSON_THROW(out_of_range(403, "key '" + key + "' not found"));
            }
        }
        else
        {
            JSON_THROW(type_error(304, "cannot use at() with " + type_name()));
        }
    }

    /*!
    @brief access specified object element with bounds checking

    Returns a const reference to the element at with specified key @a key,
    with bounds checking.

    @param[in] key  key of the element to access

    @return const reference to the element at key @a key

    @throw type_error.304 if the JSON value is not an object; in this case,
    calling `at` with a key makes no sense. See example below.
    @throw out_of_range.403 if the key @a key is is not stored in the object;
    that is, `find(key) == end()`. See example below.

    @exceptionsafety Strong guarantee: if an exception is thrown, there are no
    changes in the JSON value.

    @complexity Logarithmic in the size of the container.

    @sa @ref operator[](const typename object_t::key_type&) for unchecked
    access by reference
    @sa @ref value() for access by value with a default value

    @since version 1.0.0

    @liveexample{The example below shows how object elements can be read using
    `at()`. It also demonstrates the different exceptions that can be thrown.,
    at__object_t_key_type_const}
    */
    const_reference at(const object_t::key_type& key) const
    {
        // at only works for objects
        if (is_object())
        {
            JSON_TRY
            {
                return m_value.object->at(key);
            }
            JSON_CATCH (std::out_of_range&)
            {
                // create better exception explanation
                JSON_THROW(out_of_range(403, "key '" + key + "' not found"));
            }
        }
        else
        {
            JSON_THROW(type_error(304, "cannot use at() with " + type_name()));
        }
    }

    /*!
    @brief access specified object element

    Returns a reference to the element at with specified key @a key.

    @note If @a key is not found in the object, then it is silently added to
    the object and filled with a `null` value to make `key` a valid reference.
    In case the value was `null` before, it is converted to an object.

    @param[in] key  key of the element to access

    @return reference to the element at key @a key

    @throw type_error.305 if the JSON value is not an object or null; in that
    cases, using the [] operator with a key makes no sense.

    @complexity Logarithmic in the size of the container.

    @liveexample{The example below shows how object elements can be read and
    written using the `[]` operator.,operatorarray__key_type}

    @sa @ref at(const typename object_t::key_type&) for access by reference
    with range checking
    @sa @ref value() for access by value with a default value

    @since version 1.0.0
    */
    reference operator[](const object_t::key_type& key)
    {
        // implicitly convert null value to an empty object
        if (is_null())
        {
            m_type = value_t::object;
            m_value.object = new object_t();
            assert_invariant();
        }

        // operator[] only works for objects
        if (is_object())
        {
            return m_value.object->operator[](key);
        }

        JSON_THROW(type_error(305, "cannot use operator[] with " + type_name()));
    }

    /*!
    @brief read-only access specified object element

    Returns a const reference to the element at with specified key @a key. No
    bounds checking is performed.

    @warning If the element with key @a key does not exist, the behavior is
    undefined.

    @param[in] key  key of the element to access

    @return const reference to the element at key @a key

    @pre The element with key @a key must exist. **This precondition is
         enforced with an assertion.**

    @throw type_error.305 if the JSON value is not an object; in that cases,
    using the [] operator with a key makes no sense.

    @complexity Logarithmic in the size of the container.

    @liveexample{The example below shows how object elements can be read using
    the `[]` operator.,operatorarray__key_type_const}

    @sa @ref at(const typename object_t::key_type&) for access by reference
    with range checking
    @sa @ref value() for access by value with a default value

    @since version 1.0.0
    */
    const_reference operator[](const object_t::key_type& key) const
    {
        // const operator[] only works for objects
        if (is_object())
        {
            assert(m_value.object->find(key) != m_value.object->end());
            return m_value.object->find(key)->second;
        }

        JSON_THROW(type_error(305, "cannot use operator[] with " + type_name()));
    }

    /*!
    @brief access specified object element

    Returns a reference to the element at with specified key @a key.

    @note If @a key is not found in the object, then it is silently added to
    the object and filled with a `null` value to make `key` a valid reference.
    In case the value was `null` before, it is converted to an object.

    @param[in] key  key of the element to access

    @return reference to the element at key @a key

    @throw type_error.305 if the JSON value is not an object or null; in that
    cases, using the [] operator with a key makes no sense.

    @complexity Logarithmic in the size of the container.

    @liveexample{The example below shows how object elements can be read and
    written using the `[]` operator.,operatorarray__key_type}

    @sa @ref at(const typename object_t::key_type&) for access by reference
    with range checking
    @sa @ref value() for access by value with a default value

    @since version 1.0.0
    */
    template<typename T, std::size_t n>
    reference operator[](T * (&key)[n])
    {
        return operator[](static_cast<const T>(key));
    }

    /*!
    @brief read-only access specified object element

    Returns a const reference to the element at with specified key @a key. No
    bounds checking is performed.

    @warning If the element with key @a key does not exist, the behavior is
    undefined.

    @note This function is required for compatibility reasons with Clang.

    @param[in] key  key of the element to access

    @return const reference to the element at key @a key

    @throw type_error.305 if the JSON value is not an object; in that cases,
    using the [] operator with a key makes no sense.

    @complexity Logarithmic in the size of the container.

    @liveexample{The example below shows how object elements can be read using
    the `[]` operator.,operatorarray__key_type_const}

    @sa @ref at(const typename object_t::key_type&) for access by reference
    with range checking
    @sa @ref value() for access by value with a default value

    @since version 1.0.0
    */
    template<typename T, std::size_t n>
    const_reference operator[](T * (&key)[n]) const
    {
        return operator[](static_cast<const T>(key));
    }

    /*!
    @brief access specified object element

    Returns a reference to the element at with specified key @a key.

    @note If @a key is not found in the object, then it is silently added to
    the object and filled with a `null` value to make `key` a valid reference.
    In case the value was `null` before, it is converted to an object.

    @param[in] key  key of the element to access

    @return reference to the element at key @a key

    @throw type_error.305 if the JSON value is not an object or null; in that
    cases, using the [] operator with a key makes no sense.

    @complexity Logarithmic in the size of the container.

    @liveexample{The example below shows how object elements can be read and
    written using the `[]` operator.,operatorarray__key_type}

    @sa @ref at(const typename object_t::key_type&) for access by reference
    with range checking
    @sa @ref value() for access by value with a default value

    @since version 1.1.0
    */
    template<typename T>
    reference operator[](T* key)
    {
        // implicitly convert null to object
        if (is_null())
        {
            m_type = value_t::object;
            m_value = value_t::object;
            assert_invariant();
        }

        // at only works for objects
        if (is_object())
        {
            return m_value.object->operator[](key);
        }

        JSON_THROW(type_error(305, "cannot use operator[] with " + type_name()));
    }

    /*!
    @brief read-only access specified object element

    Returns a const reference to the element at with specified key @a key. No
    bounds checking is performed.

    @warning If the element with key @a key does not exist, the behavior is
    undefined.

    @param[in] key  key of the element to access

    @return const reference to the element at key @a key

    @pre The element with key @a key must exist. **This precondition is
         enforced with an assertion.**

    @throw type_error.305 if the JSON value is not an object; in that cases,
    using the [] operator with a key makes no sense.

    @complexity Logarithmic in the size of the container.

    @liveexample{The example below shows how object elements can be read using
    the `[]` operator.,operatorarray__key_type_const}

    @sa @ref at(const typename object_t::key_type&) for access by reference
    with range checking
    @sa @ref value() for access by value with a default value

    @since version 1.1.0
    */
    template<typename T>
    const_reference operator[](T* key) const
    {
        // at only works for objects
        if (is_object())
        {
            assert(m_value.object->find(key) != m_value.object->end());
            return m_value.object->find(key)->second;
        }

        JSON_THROW(type_error(305, "cannot use operator[] with " + type_name()));
    }

    /// @}


    //////////////
    // capacity //
    //////////////

    /// @name capacity
    /// @{

    /*!
    @brief checks whether the container is empty

    Checks if a JSON value has no elements.

    @return The return value depends on the different types and is
            defined as follows:
            Value type  | return value
            ----------- | -------------
            null        | `true`
            string      | `false`
            object      | result of function `object_t::empty()`

    @note This function does not return whether a string stored as JSON value
    is empty - it returns whether the JSON container itself is empty which is
    false in the case of a string.

    @complexity Constant, as long as @ref array_t and @ref object_t satisfy
    the Container concept; that is, their `empty()` functions have constant
    complexity.

    @requirement This function helps `basic_json` satisfying the
    [Container](http://en.cppreference.com/w/cpp/concept/Container)
    requirements:
    - The complexity is constant.
    - Has the semantics of `begin() == end()`.

    @liveexample{The following code uses `empty()` to check if a JSON
    object contains any elements.,empty}

    @sa @ref size() -- returns the number of elements

    @since version 1.0.0
    */
    bool empty() const noexcept
    {
        switch (m_type)
        {
            case value_t::null:
            {
                // null values are empty
                return true;
            }

            case value_t::object:
            {
                // delegate call to object_t::empty()
                return m_value.object->empty();
            }

            default:
            {
                // all other types are nonempty
                return false;
            }
        }
    }

    /*!
    @brief returns the number of elements

    Returns the number of elements in a JSON value.

    @return The return value depends on the different types and is
            defined as follows:
            Value type  | return value
            ----------- | -------------
            null        | `0`
            string      | `1`
            object      | result of function object_t::size()

    @note This function does not return the length of a string stored as JSON
    value - it returns the number of elements in the JSON value which is 1 in
    the case of a string.

    @complexity Constant, as long as @ref array_t and @ref object_t satisfy
    the Container concept; that is, their size() functions have constant
    complexity.

    @requirement This function helps `basic_json` satisfying the
    [Container](http://en.cppreference.com/w/cpp/concept/Container)
    requirements:
    - The complexity is constant.
    - Has the semantics of `std::distance(begin(), end())`.

    @liveexample{The following code calls `size()` on the different value
    types.,size}

    @sa @ref empty() -- checks whether the container is empty
    @sa @ref max_size() -- returns the maximal number of elements

    @since version 1.0.0
    */
    size_type size() const noexcept
    {
        switch (m_type)
        {
            case value_t::null:
            {
                // null values are empty
                return 0;
            }

            case value_t::object:
            {
                // delegate call to object_t::size()
                return m_value.object->size();
            }

            default:
            {
                // all other types have size 1
                return 1;
            }
        }
    }

    /*!
    @brief returns the maximum possible number of elements

    Returns the maximum number of elements a JSON value is able to hold due to
    system or library implementation limitations, i.e. `std::distance(begin(),
    end())` for the JSON value.

    @return The return value depends on the different types and is
            defined as follows:
            Value type  | return value
            ----------- | -------------
            null        | `0` (same as `size()`)
            boolean     | `1` (same as `size()`)
            string      | `1` (same as `size()`)
            number      | `1` (same as `size()`)
            object      | result of function `object_t::max_size()`
            array       | result of function `array_t::max_size()`

    @complexity Constant, as long as @ref array_t and @ref object_t satisfy
    the Container concept; that is, their `max_size()` functions have constant
    complexity.

    @requirement This function helps `basic_json` satisfying the
    [Container](http://en.cppreference.com/w/cpp/concept/Container)
    requirements:
    - The complexity is constant.
    - Has the semantics of returning `b.size()` where `b` is the largest
      possible JSON value.

    @liveexample{The following code calls `max_size()` on the different value
    types. Note the output is implementation specific.,max_size}

    @sa @ref size() -- returns the number of elements

    @since version 1.0.0
    */
    size_type max_size() const noexcept
    {
        switch (m_type)
        {
            case value_t::object:
            {
                // delegate call to object_t::max_size()
                return m_value.object->max_size();
            }

            default:
            {
                // all other types have max_size() == size()
                return size();
            }
        }
    }

    /// @}


    ///////////////
    // modifiers //
    ///////////////

    /// @name modifiers
    /// @{

    /*!
    @brief clears the contents

    Clears the content of a JSON value and resets it to the default value as
    if @ref basic_json(value_t) would have been called:

    Value type  | initial value
    ----------- | -------------
    null        | `null`
    boolean     | `false`
    string      | `""`
    number      | `0`
    object      | `{}`
    array       | `[]`

    @complexity Linear in the size of the JSON value.

    @liveexample{The example below shows the effect of `clear()` to different
    JSON types.,clear}

    @since version 1.0.0
    */
    void clear() noexcept
    {
        switch (m_type)
        {
            case value_t::string:
            {
                m_value.string->clear();
                break;
            }

            case value_t::object:
            {
                m_value.object->clear();
                break;
            }

            default:
            {
                break;
            }
        }
    }

    /*!
    @brief add an object to an object

    Inserts the given element @a val to the JSON object. If the function is
    called on a JSON null value, an empty object is created before inserting
    @a val.

    @param[in] val the value to add to the JSON object

    @throw type_error.308 when called on a type other than JSON object or
    null; example: `"cannot use push_back() with number"`

    @complexity Logarithmic in the size of the container, O(log(`size()`)).

    @liveexample{The example shows how `push_back()` and `+=` can be used to
    add elements to a JSON object. Note how the `null` value was silently
    converted to a JSON object.,push_back__object_t__value}

    @since version 1.0.0
    */
    void push_back(const object_t::value_type& val)
    {
        // push_back only works for null objects or objects
        if (not(is_null() or is_object()))
        {
            JSON_THROW(type_error(308, "cannot use push_back() with " + type_name()));
        }

        // transform null object into an object
        if (is_null())
        {
            m_type = value_t::object;
            m_value = value_t::object;
            assert_invariant();
        }

        // add element to array
        m_value.object->insert(val);
    }

    /*!
    @brief add an object to an object
    @copydoc push_back(const typename object_t::value_type&)
    */
    reference operator+=(const object_t::value_type& val)
    {
        push_back(val);
        return *this;
    }

    /*!
    @brief exchanges the values

    Exchanges the contents of the JSON value with those of @a other. Does not
    invoke any move, copy, or swap operations on individual elements. All
    iterators and references remain valid. The past-the-end iterator is
    invalidated.

    @param[in,out] other JSON value to exchange the contents with

    @complexity Constant.

    @liveexample{The example below shows how JSON values can be swapped with
    `swap()`.,swap__reference}

    @since version 1.0.0
    */
    void swap(reference other)
    {
        std::swap(m_type, other.m_type);
        std::swap(m_value, other.m_value);
        assert_invariant();
    }

    /*!
    @brief exchanges the values

    Exchanges the contents of a JSON object with those of @a other. Does not
    invoke any move, copy, or swap operations on individual elements. All
    iterators and references remain valid. The past-the-end iterator is
    invalidated.

    @param[in,out] other object to exchange the contents with

    @throw type_error.310 when JSON value is not an object; example:
    `"cannot use swap() with string"`

    @complexity Constant.

    @liveexample{The example below shows how objects can be swapped with
    `swap()`.,swap__object_t}

    @since version 1.0.0
    */
    void swap(object_t& other)
    {
        // swap only works for objects
        if (is_object())
        {
            std::swap(*(m_value.object), other);
        }
        else
        {
            JSON_THROW(type_error(310, "cannot use swap() with " + type_name()));
        }
    }

    /*!
    @brief exchanges the values

    Exchanges the contents of a JSON string with those of @a other. Does not
    invoke any move, copy, or swap operations on individual elements. All
    iterators and references remain valid. The past-the-end iterator is
    invalidated.

    @param[in,out] other string to exchange the contents with

    @throw type_error.310 when JSON value is not a string; example: `"cannot
    use swap() with boolean"`

    @complexity Constant.

    @liveexample{The example below shows how strings can be swapped with
    `swap()`.,swap__string_t}

    @since version 1.0.0
    */
    void swap(string_t& other)
    {
        // swap only works for strings
        if (is_string())
        {
            std::swap(*(m_value.string), other);
        }
        else
        {
            JSON_THROW(type_error(310, "cannot use swap() with " + type_name()));
        }
    }

    /// @}

  public:
    //////////////////////////////////////////
    // lexicographical comparison operators //
    //////////////////////////////////////////

    /// @name lexicographical comparison operators
    /// @{

    /*!
    @brief comparison: equal

    Compares two JSON values for equality according to the following rules:
    - Two JSON values are equal if (1) they are from the same type and (2)
      their stored values are the same.
    - Two JSON null values are equal.

    @param[in] lhs  first JSON value to consider
    @param[in] rhs  second JSON value to consider
    @return whether the values @a lhs and @a rhs are equal

    @complexity Linear.

    @liveexample{The example demonstrates comparing several JSON
    types.,operator__equal}

    @since version 1.0.0
    */
    friend bool operator==(const_reference lhs, const_reference rhs) noexcept
    {
        const auto lhs_type = lhs.type();
        const auto rhs_type = rhs.type();

        if (lhs_type == rhs_type)
        {
            switch (lhs_type)
            {
                case value_t::object:
                {
                    return *lhs.m_value.object == *rhs.m_value.object;
                }
                case value_t::null:
                {
                    return true;
                }
                case value_t::string:
                {
                    return *lhs.m_value.string == *rhs.m_value.string;
                }
                default:
                {
                    return false;
                }
            }
        }

        return false;
    }

    /// @}


    ///////////////////
    // serialization //
    ///////////////////

    /// @name serialization
    /// @{

  private:
    /*!
    @brief wrapper around the serialization functions
    */
    class serializer
    {
      private:
        serializer();
        serializer(const serializer&);
        serializer& operator=(const serializer&);

      public:
        /*!
        @param[in] s  output stream to serialize to
        */
        serializer(std::ostream& s)
            : o(s), loc(std::localeconv()),
              thousands_sep(!loc->thousands_sep ? '\0' : loc->thousands_sep[0]),
              decimal_point(!loc->decimal_point ? '\0' : loc->decimal_point[0]),
              indent_string(512, ' ')
        {}

        /*!
        @brief internal implementation of the serialization function

        This function is called by the public member function dump and
        organizes the serialization internally. The indentation level is
        propagated as additional parameter. In case of arrays and objects, the
        function is called recursively.

        - strings and object keys are escaped using `escape_string()`
        - integer numbers are converted implicitly via `operator<<`
        - floating-point numbers are converted to a string using `"%g"` format

        @param[in] val             value to serialize
        @param[in] pretty_print    whether the output shall be pretty-printed
        @param[in] indent_step     the indent level
        @param[in] current_indent  the current indent level (only used internally)
        */
        void dump(const basic_json& val,
                  const bool pretty_print,
                  const unsigned int indent_step,
                  const unsigned int current_indent = 0)
        {
            switch (val.m_type)
            {
                case value_t::object:
                {
                    if (val.m_value.object->empty())
                    {
                        o.write("{}", 2);
                        return;
                    }

                    if (pretty_print)
                    {
                        o.write("{\n", 2);

                        // variable to hold indentation for recursive calls
                        const auto new_indent = current_indent + indent_step;
                        if (indent_string.size() < new_indent)
                        {
                            indent_string.resize(new_indent, ' ');
                        }

                        // first n-1 elements
                        auto i = val.m_value.object->cbegin();
                        for (size_t cnt = 0; cnt < val.m_value.object->size() - 1; ++cnt, ++i)
                        {
                            o.write(indent_string.c_str(), static_cast<std::streamsize>(new_indent));
                            o.put('\"');
                            dump_escaped(i->first);
                            o.write("\": ", 3);
                            dump(i->second, true, indent_step, new_indent);
                            o.write(",\n", 2);
                        }

                        // last element
                        assert(i != val.m_value.object->cend());
                        o.write(indent_string.c_str(), static_cast<std::streamsize>(new_indent));
                        o.put('\"');
                        dump_escaped(i->first);
                        o.write("\": ", 3);
                        dump(i->second, true, indent_step, new_indent);

                        o.put('\n');
                        o.write(indent_string.c_str(), static_cast<std::streamsize>(current_indent));
                        o.put('}');
                    }
                    else
                    {
                        o.put('{');

                        // first n-1 elements
                        auto i = val.m_value.object->cbegin();
                        for (size_t cnt = 0; cnt < val.m_value.object->size() - 1; ++cnt, ++i)
                        {
                            o.put('\"');
                            dump_escaped(i->first);
                            o.write("\":", 2);
                            dump(i->second, false, indent_step, current_indent);
                            o.put(',');
                        }

                        // last element
                        assert(i != val.m_value.object->cend());
                        o.put('\"');
                        dump_escaped(i->first);
                        o.write("\":", 2);
                        dump(i->second, false, indent_step, current_indent);

                        o.put('}');
                    }

                    return;
                }


                case value_t::string:
                {
                    o.put('\"');
                    dump_escaped(*val.m_value.string);
                    o.put('\"');
                    return;
                }

                case value_t::discarded:
                {
                    o.write("<discarded>", 11);
                    return;
                }

                case value_t::null:
                {
                    o.write("null", 4);
                    return;
                }
            }
        }

      private:
        /*!
        @brief calculates the extra space to escape a JSON string

        @param[in] s  the string to escape
        @return the number of characters required to escape string @a s

        @complexity Linear in the length of string @a s.
        */
        static std::size_t extra_space(const string_t& s) noexcept
        {
            return std::accumulate(s.begin(), s.end(), size_t(),
                                   [](size_t res, string_t::value_type c)
            {
                switch (c)
                {
                    case '"':
                    case '\\':
                    case '\b':
                    case '\f':
                    case '\n':
                    case '\r':
                    case '\t':
                    {
                        // from c (1 byte) to \x (2 bytes)
                        return res + 1;
                    }

                    case 0x00:
                    case 0x01:
                    case 0x02:
                    case 0x03:
                    case 0x04:
                    case 0x05:
                    case 0x06:
                    case 0x07:
                    case 0x0b:
                    case 0x0e:
                    case 0x0f:
                    case 0x10:
                    case 0x11:
                    case 0x12:
                    case 0x13:
                    case 0x14:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                    case 0x18:
                    case 0x19:
                    case 0x1a:
                    case 0x1b:
                    case 0x1c:
                    case 0x1d:
                    case 0x1e:
                    case 0x1f:
                    {
                        // from c (1 byte) to \uxxxx (6 bytes)
                        return res + 5;
                    }

                    default:
                    {
                        return res;
                    }
                }
            });
        }

        /*!
        @brief dump escaped string

        Escape a string by replacing certain special characters by a sequence
        of an escape character (backslash) and another character and other
        control characters by a sequence of "\u" followed by a four-digit hex
        representation. The escaped string is written to output stream @a o.

        @param[in] s  the string to escape

        @complexity Linear in the length of string @a s.
        */
        void dump_escaped(const string_t& s) const
        {
            const auto space = extra_space(s);
            if (space == 0)
            {
                o.write(s.c_str(), static_cast<std::streamsize>(s.size()));
                return;
            }

            // create a result string of necessary size
            string_t result(s.size() + space, '\\');
            std::size_t pos = 0;

            for (const auto& c : s)
            {
                switch (c)
                {
                    // quotation mark (0x22)
                    case '"':
                    {
                        result[pos + 1] = '"';
                        pos += 2;
                        break;
                    }

                    // reverse solidus (0x5c)
                    case '\\':
                    {
                        // nothing to change
                        pos += 2;
                        break;
                    }

                    // backspace (0x08)
                    case '\b':
                    {
                        result[pos + 1] = 'b';
                        pos += 2;
                        break;
                    }

                    // formfeed (0x0c)
                    case '\f':
                    {
                        result[pos + 1] = 'f';
                        pos += 2;
                        break;
                    }

                    // newline (0x0a)
                    case '\n':
                    {
                        result[pos + 1] = 'n';
                        pos += 2;
                        break;
                    }

                    // carriage return (0x0d)
                    case '\r':
                    {
                        result[pos + 1] = 'r';
                        pos += 2;
                        break;
                    }

                    // horizontal tab (0x09)
                    case '\t':
                    {
                        result[pos + 1] = 't';
                        pos += 2;
                        break;
                    }

                    case 0x00:
                    case 0x01:
                    case 0x02:
                    case 0x03:
                    case 0x04:
                    case 0x05:
                    case 0x06:
                    case 0x07:
                    case 0x0b:
                    case 0x0e:
                    case 0x0f:
                    case 0x10:
                    case 0x11:
                    case 0x12:
                    case 0x13:
                    case 0x14:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                    case 0x18:
                    case 0x19:
                    case 0x1a:
                    case 0x1b:
                    case 0x1c:
                    case 0x1d:
                    case 0x1e:
                    case 0x1f:
                    {
                        // convert a number 0..15 to its hex representation
                        // (0..f)
                        static const char hexify[16] =
                        {
                            '0', '1', '2', '3', '4', '5', '6', '7',
                            '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
                        };

                        // print character c as \uxxxx
                        result[++pos] = 'u';
                        result[++pos] = '0';
                        result[++pos] = '0';
                        result[++pos] = hexify[c >> 4];
                        result[++pos] = hexify[c & 0x0f];

                        ++pos;
                        break;
                    }

                    default:
                    {
                        // all other characters are added as-is
                        result[pos++] = c;
                        break;
                    }
                }
            }

            assert(pos == s.size() + space);
            o.write(result.c_str(), static_cast<std::streamsize>(result.size()));
        }

      private:
        /// the output of the serializer
        std::ostream& o;

        /// the locale
        const std::lconv* loc;
        /// the locale's thousand separator character
        const char thousands_sep;
        /// the locale's decimal point character
        const char decimal_point;

        /// the indentation string
        string_t indent_string;
    };

    public:
    /// @}


    /////////////////////
    // deserialization //
    /////////////////////

    /// @name deserialization
    /// @{

    /*!
    @brief deserialize from string literal

    @tparam CharT character/literal type with size of 1 byte
    @param[in] s  string literal to read a serialized JSON value from
    @param[in] cb a parser callback function of type @ref parser_callback_t
    which is used to control the deserialization by filtering unwanted values
    (optional)

    @return result of the deserialization

    @throw parse_error.101 in case of an unexpected token
    @throw parse_error.102 if to_unicode fails or surrogate error
    @throw parse_error.103 if to_unicode fails

    @complexity Linear in the length of the input. The parser is a predictive
    LL(1) parser. The complexity can be higher if the parser callback function
    @a cb has a super-linear complexity.

    @note A UTF-8 byte order mark is silently ignored.
    @note String containers like `std::string` or @ref string_t can be parsed
          with @ref parse(const ContiguousContainer&, const parser_callback_t)

    @liveexample{The example below demonstrates the `parse()` function with
    and without callback function.,parse__string__parser_callback_t}

    @sa @ref parse(std::istream&, const parser_callback_t) for a version that
    reads from an input stream

    @since version 1.0.0 (originally for @ref string_t)
    */
    template<typename CharT>
    static basic_json parse(const CharT s,
                            const parser_callback_t cb = nullptr)
    {
        return parser(reinterpret_cast<const char*>(s), cb).parse();
    }
    /// @}


    ///////////////////////////
    // convenience functions //
    ///////////////////////////

    /*!
    @brief return the type as string

    Returns the type name as string to be used in error messages - usually to
    indicate that a function was called on a wrong JSON type.

    @return basically a string representation of a the @a m_type member

    @complexity Constant.

    @liveexample{The following code exemplifies `type_name()` for all JSON
    types.,type_name}

    @since version 1.0.0, public since 2.1.0
    */
    std::string type_name() const
    {
        {
            switch (m_type)
            {
                case value_t::null:
                    return "null";
                case value_t::object:
                    return "object";
                case value_t::string:
                    return "string";
                case value_t::discarded:
                default:
                    return "discarded";
            }
        }
    }


  private:
    //////////////////////
    // member variables //
    //////////////////////

    /// the type of the current element
    value_t m_type;

    /// the value of the current element
    json_value m_value;


  private:
    //////////////////////
    // lexer and parser //
    //////////////////////

    /*!
    @brief lexical analysis

    This class organizes the lexical analysis during JSON deserialization. The
    core of it is a scanner generated by [re2c](http://re2c.org) that
    processes a buffer and recognizes tokens according to RFC 7159.
    */
    class lexer
    {
      public:
        /// token types for the parser
        enum class token_type
        {
            uninitialized,   ///< indicating the scanner is uninitialized
            literal_true,    ///< the `true` literal
            literal_false,   ///< the `false` literal
            literal_null,    ///< the `null` literal
            value_string,    ///< a string -- use get_string() for actual value
            value_unsigned,  ///< an unsigned integer -- use get_number() for actual value
            value_integer,   ///< a signed integer -- use get_number() for actual value
            value_float,     ///< an floating point number -- use get_number() for actual value
            begin_array,     ///< the character for array begin `[`
            begin_object,    ///< the character for object begin `{`
            end_array,       ///< the character for array end `]`
            end_object,      ///< the character for object end `}`
            name_separator,  ///< the name separator `:`
            value_separator, ///< the value separator `,`
            parse_error,     ///< indicating a parse error
            end_of_input     ///< indicating the end of the input buffer
        };

        /// the char type to use in the lexer
        typedef unsigned char lexer_char_t;

        /// a lexer from a buffer with given length
        lexer(const lexer_char_t* buff, const size_t len) noexcept
            : m_content(buff)
            , m_stream(nullptr)
            , m_marker(nullptr)
            , last_token_type(token_type::end_of_input)
            , position(0)
        {
            assert(m_content != nullptr);
            m_start = m_cursor = m_content;
            m_limit = m_content + len;
        }

        /*!
        @brief a lexer from an input stream
        @throw parse_error.111 if input stream is in a bad state
        */
        explicit lexer(std::istream& s)
            : m_stream(&s), m_line_buffer()
        {
            // immediately abort if stream is erroneous
            if (s.fail())
            {
                JSON_THROW(parse_error(111, 0, "bad input stream"));
            }

            // fill buffer
            fill_line_buffer();

            // skip UTF-8 byte-order mark
            if (m_line_buffer.size() >= 3 and m_line_buffer.substr(0, 3) == "\xEF\xBB\xBF")
            {
                m_line_buffer[0] = ' ';
                m_line_buffer[1] = ' ';
                m_line_buffer[2] = ' ';
            }
        }

    private:
        // switch off unwanted functions (due to pointer members)
        lexer();
        lexer(const lexer&);
        lexer operator=(const lexer&);
    public:
        /*!
        @brief create a string from one or two Unicode code points

        There are two cases: (1) @a codepoint1 is in the Basic Multilingual
        Plane (U+0000 through U+FFFF) and @a codepoint2 is 0, or (2)
        @a codepoint1 and @a codepoint2 are a UTF-16 surrogate pair to
        represent a code point above U+FFFF.

        @param[in] codepoint1  the code point (can be high surrogate)
        @param[in] codepoint2  the code point (can be low surrogate or 0)

        @return string representation of the code point; the length of the
        result string is between 1 and 4 characters.

        @throw parse_error.102 if the low surrogate is invalid; example:
        `""missing or wrong low surrogate""`
        @throw parse_error.103 if code point is > 0x10ffff; example: `"code
        points above 0x10FFFF are invalid"`

        @complexity Constant.

        @see <http://en.wikipedia.org/wiki/UTF-8#Sample_code>
        */
        string_t to_unicode(const std::size_t codepoint1,
                            const std::size_t codepoint2 = 0) const
        {
            // calculate the code point from the given code points
            std::size_t codepoint = codepoint1;

            // check if codepoint1 is a high surrogate
            if (codepoint1 >= 0xD800 and codepoint1 <= 0xDBFF)
            {
                // check if codepoint2 is a low surrogate
                if (codepoint2 >= 0xDC00 and codepoint2 <= 0xDFFF)
                {
                    codepoint =
                        // high surrogate occupies the most significant 22 bits
                        (codepoint1 << 10)
                        // low surrogate occupies the least significant 15 bits
                        + codepoint2
                        // there is still the 0xD800, 0xDC00 and 0x10000 noise
                        // in the result so we have to subtract with:
                        // (0xD800 << 10) + DC00 - 0x10000 = 0x35FDC00
                        - 0x35FDC00;
                }
                else
                {
                    JSON_THROW(parse_error(102, get_position(), "missing or wrong low surrogate"));
                }
            }

            string_t result;

            if (codepoint < 0x80)
            {
                // 1-byte characters: 0xxxxxxx (ASCII)
                result.append(1, static_cast<string_t::value_type>(codepoint));
            }
            else if (codepoint <= 0x7ff)
            {
                // 2-byte characters: 110xxxxx 10xxxxxx
                result.append(1, static_cast<string_t::value_type>(0xC0 | ((codepoint >> 6) & 0x1F)));
                result.append(1, static_cast<string_t::value_type>(0x80 | (codepoint & 0x3F)));
            }
            else if (codepoint <= 0xffff)
            {
                // 3-byte characters: 1110xxxx 10xxxxxx 10xxxxxx
                result.append(1, static_cast<string_t::value_type>(0xE0 | ((codepoint >> 12) & 0x0F)));
                result.append(1, static_cast<string_t::value_type>(0x80 | ((codepoint >> 6) & 0x3F)));
                result.append(1, static_cast<string_t::value_type>(0x80 | (codepoint & 0x3F)));
            }
            else if (codepoint <= 0x10ffff)
            {
                // 4-byte characters: 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
                result.append(1, static_cast<string_t::value_type>(0xF0 | ((codepoint >> 18) & 0x07)));
                result.append(1, static_cast<string_t::value_type>(0x80 | ((codepoint >> 12) & 0x3F)));
                result.append(1, static_cast<string_t::value_type>(0x80 | ((codepoint >> 6) & 0x3F)));
                result.append(1, static_cast<string_t::value_type>(0x80 | (codepoint & 0x3F)));
            }
            else
            {
                JSON_THROW(parse_error(103, get_position(), "code points above 0x10FFFF are invalid"));
            }

            return result;
        }

        /// return name of values of type token_type (only used for errors)
        static std::string token_type_name(const token_type t)
        {
            switch (t)
            {
                case token_type::uninitialized:
                    return "<uninitialized>";
                case token_type::literal_true:
                    return "true literal";
                case token_type::literal_false:
                    return "false literal";
                case token_type::literal_null:
                    return "null literal";
                case token_type::value_string:
                    return "string literal";
                case lexer::token_type::value_unsigned:
                case lexer::token_type::value_integer:
                case lexer::token_type::value_float:
                    return "number literal";
                case token_type::begin_array:
                    return "'['";
                case token_type::begin_object:
                    return "'{'";
                case token_type::end_array:
                    return "']'";
                case token_type::end_object:
                    return "'}'";
                case token_type::name_separator:
                    return "':'";
                case token_type::value_separator:
                    return "','";
                case token_type::parse_error:
                    return "<parse error>";
                case token_type::end_of_input:
                    return "end of input";
                default:
                {
                    // catch non-enum values
                    return "unknown token"; // LCOV_EXCL_LINE
                }
            }
        }

        /*!
        This function implements a scanner for JSON. It is specified using
        regular expressions that try to follow RFC 7159 as close as possible.
        These regular expressions are then translated into a minimized
        deterministic finite automaton (DFA) by the tool
        [re2c](http://re2c.org). As a result, the translated code for this
        function consists of a large block of code with `goto` jumps.

        @return the class of the next token read from the buffer

        @complexity Linear in the length of the input.\n

        Proposition: The loop below will always terminate for finite input.\n

        Proof (by contradiction): Assume a finite input. To loop forever, the
        loop must never hit code with a `break` statement. The only code
        snippets without a `break` statement is the continue statement for
        whitespace. To loop forever, the input must be an infinite sequence
        whitespace. This contradicts the assumption of finite input, q.e.d.
        */
        token_type scan()
        {
            for (;;)
            {
                // pointer for backtracking information
                m_marker = nullptr;

                // remember the begin of the token
                m_start = m_cursor;
                assert(m_start != nullptr);


                {
                    lexer_char_t yych;
                    unsigned int yyaccept = 0;
                    static const unsigned char yybm[] =
                    {
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,  32,  32,   0,   0,  32,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        160, 128,   0, 128, 128, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        192, 192, 192, 192, 192, 192, 192, 192,
                        192, 192, 128, 128, 128, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        128, 128, 128, 128,   0, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        128, 128, 128, 128, 128, 128, 128, 128,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                        0,   0,   0,   0,   0,   0,   0,   0,
                    };
                    if ((m_limit - m_cursor) < 5)
                    {
                        fill_line_buffer(5);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yybm[0 + yych] & 32)
                    {
                        goto basic_json_parser_6;
                    }
                    if (yych <= '[')
                    {
                        if (yych <= '-')
                        {
                            if (yych <= '"')
                            {
                                if (yych <= 0x00)
                                {
                                    goto basic_json_parser_2;
                                }
                                if (yych <= '!')
                                {
                                    goto basic_json_parser_4;
                                }
                                goto basic_json_parser_9;
                            }
                            else
                            {
                                if (yych <= '+')
                                {
                                    goto basic_json_parser_4;
                                }
                                if (yych <= ',')
                                {
                                    goto basic_json_parser_10;
                                }
                                goto basic_json_parser_12;
                            }
                        }
                        else
                        {
                            if (yych <= '9')
                            {
                                if (yych <= '/')
                                {
                                    goto basic_json_parser_4;
                                }
                                if (yych <= '0')
                                {
                                    goto basic_json_parser_13;
                                }
                                goto basic_json_parser_15;
                            }
                            else
                            {
                                if (yych <= ':')
                                {
                                    goto basic_json_parser_17;
                                }
                                if (yych <= 'Z')
                                {
                                    goto basic_json_parser_4;
                                }
                                goto basic_json_parser_19;
                            }
                        }
                    }
                    else
                    {
                        if (yych <= 'n')
                        {
                            if (yych <= 'e')
                            {
                                if (yych == ']')
                                {
                                    goto basic_json_parser_21;
                                }
                                goto basic_json_parser_4;
                            }
                            else
                            {
                                if (yych <= 'f')
                                {
                                    goto basic_json_parser_23;
                                }
                                if (yych <= 'm')
                                {
                                    goto basic_json_parser_4;
                                }
                                goto basic_json_parser_24;
                            }
                        }
                        else
                        {
                            if (yych <= 'z')
                            {
                                if (yych == 't')
                                {
                                    goto basic_json_parser_25;
                                }
                                goto basic_json_parser_4;
                            }
                            else
                            {
                                if (yych <= '{')
                                {
                                    goto basic_json_parser_26;
                                }
                                if (yych == '}')
                                {
                                    goto basic_json_parser_28;
                                }
                                goto basic_json_parser_4;
                            }
                        }
                    }
basic_json_parser_2:
                    ++m_cursor;
                    {
                        last_token_type = token_type::end_of_input;
                        break;
                    }
basic_json_parser_4:
                    ++m_cursor;
basic_json_parser_5:
                    {
                        last_token_type = token_type::parse_error;
                        break;
                    }
basic_json_parser_6:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yybm[0 + yych] & 32)
                    {
                        goto basic_json_parser_6;
                    }
                    {
                        position += static_cast<size_t>((m_cursor - m_start));
                        continue;
                    }
basic_json_parser_9:
                    yyaccept = 0;
                    yych = *(m_marker = ++m_cursor);
                    if (yych <= 0x1F)
                    {
                        goto basic_json_parser_5;
                    }
                    if (yych <= 0x7F)
                    {
                        goto basic_json_parser_31;
                    }
                    if (yych <= 0xC1)
                    {
                        goto basic_json_parser_5;
                    }
                    if (yych <= 0xF4)
                    {
                        goto basic_json_parser_31;
                    }
                    goto basic_json_parser_5;
basic_json_parser_10:
                    ++m_cursor;
                    {
                        last_token_type = token_type::value_separator;
                        break;
                    }
basic_json_parser_12:
                    yych = *++m_cursor;
                    if (yych <= '/')
                    {
                        goto basic_json_parser_5;
                    }
                    if (yych <= '0')
                    {
                        goto basic_json_parser_43;
                    }
                    if (yych <= '9')
                    {
                        goto basic_json_parser_45;
                    }
                    goto basic_json_parser_5;
basic_json_parser_13:
                    yyaccept = 1;
                    yych = *(m_marker = ++m_cursor);
                    if (yych <= '9')
                    {
                        if (yych == '.')
                        {
                            goto basic_json_parser_47;
                        }
                        if (yych >= '0')
                        {
                            goto basic_json_parser_48;
                        }
                    }
                    else
                    {
                        if (yych <= 'E')
                        {
                            if (yych >= 'E')
                            {
                                goto basic_json_parser_51;
                            }
                        }
                        else
                        {
                            if (yych == 'e')
                            {
                                goto basic_json_parser_51;
                            }
                        }
                    }
basic_json_parser_14:
                    {
                        last_token_type = token_type::value_unsigned;
                        break;
                    }
basic_json_parser_15:
                    yyaccept = 1;
                    m_marker = ++m_cursor;
                    if ((m_limit - m_cursor) < 3)
                    {
                        fill_line_buffer(3);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yybm[0 + yych] & 64)
                    {
                        goto basic_json_parser_15;
                    }
                    if (yych <= 'D')
                    {
                        if (yych == '.')
                        {
                            goto basic_json_parser_47;
                        }
                        goto basic_json_parser_14;
                    }
                    else
                    {
                        if (yych <= 'E')
                        {
                            goto basic_json_parser_51;
                        }
                        if (yych == 'e')
                        {
                            goto basic_json_parser_51;
                        }
                        goto basic_json_parser_14;
                    }
basic_json_parser_17:
                    ++m_cursor;
                    {
                        last_token_type = token_type::name_separator;
                        break;
                    }
basic_json_parser_19:
                    ++m_cursor;
                    {
                        last_token_type = token_type::begin_array;
                        break;
                    }
basic_json_parser_21:
                    ++m_cursor;
                    {
                        last_token_type = token_type::end_array;
                        break;
                    }
basic_json_parser_23:
                    yyaccept = 0;
                    yych = *(m_marker = ++m_cursor);
                    if (yych == 'a')
                    {
                        goto basic_json_parser_52;
                    }
                    goto basic_json_parser_5;
basic_json_parser_24:
                    yyaccept = 0;
                    yych = *(m_marker = ++m_cursor);
                    if (yych == 'u')
                    {
                        goto basic_json_parser_53;
                    }
                    goto basic_json_parser_5;
basic_json_parser_25:
                    yyaccept = 0;
                    yych = *(m_marker = ++m_cursor);
                    if (yych == 'r')
                    {
                        goto basic_json_parser_54;
                    }
                    goto basic_json_parser_5;
basic_json_parser_26:
                    ++m_cursor;
                    {
                        last_token_type = token_type::begin_object;
                        break;
                    }
basic_json_parser_28:
                    ++m_cursor;
                    {
                        last_token_type = token_type::end_object;
                        break;
                    }
basic_json_parser_30:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
basic_json_parser_31:
                    if (yybm[0 + yych] & 128)
                    {
                        goto basic_json_parser_30;
                    }
                    if (yych <= 0xE0)
                    {
                        if (yych <= '\\')
                        {
                            if (yych <= 0x1F)
                            {
                                goto basic_json_parser_32;
                            }
                            if (yych <= '"')
                            {
                                goto basic_json_parser_33;
                            }
                            goto basic_json_parser_35;
                        }
                        else
                        {
                            if (yych <= 0xC1)
                            {
                                goto basic_json_parser_32;
                            }
                            if (yych <= 0xDF)
                            {
                                goto basic_json_parser_36;
                            }
                            goto basic_json_parser_37;
                        }
                    }
                    else
                    {
                        if (yych <= 0xEF)
                        {
                            if (yych == 0xED)
                            {
                                goto basic_json_parser_39;
                            }
                            goto basic_json_parser_38;
                        }
                        else
                        {
                            if (yych <= 0xF0)
                            {
                                goto basic_json_parser_40;
                            }
                            if (yych <= 0xF3)
                            {
                                goto basic_json_parser_41;
                            }
                            if (yych <= 0xF4)
                            {
                                goto basic_json_parser_42;
                            }
                        }
                    }
basic_json_parser_32:
                    m_cursor = m_marker;
                    if (yyaccept <= 1)
                    {
                        if (yyaccept == 0)
                        {
                            goto basic_json_parser_5;
                        }
                        else
                        {
                            goto basic_json_parser_14;
                        }
                    }
                    else
                    {
                        if (yyaccept == 2)
                        {
                            goto basic_json_parser_44;
                        }
                        else
                        {
                            goto basic_json_parser_58;
                        }
                    }
basic_json_parser_33:
                    ++m_cursor;
                    {
                        last_token_type = token_type::value_string;
                        break;
                    }
basic_json_parser_35:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 'e')
                    {
                        if (yych <= '/')
                        {
                            if (yych == '"')
                            {
                                goto basic_json_parser_30;
                            }
                            if (yych <= '.')
                            {
                                goto basic_json_parser_32;
                            }
                            goto basic_json_parser_30;
                        }
                        else
                        {
                            if (yych <= '\\')
                            {
                                if (yych <= '[')
                                {
                                    goto basic_json_parser_32;
                                }
                                goto basic_json_parser_30;
                            }
                            else
                            {
                                if (yych == 'b')
                                {
                                    goto basic_json_parser_30;
                                }
                                goto basic_json_parser_32;
                            }
                        }
                    }
                    else
                    {
                        if (yych <= 'q')
                        {
                            if (yych <= 'f')
                            {
                                goto basic_json_parser_30;
                            }
                            if (yych == 'n')
                            {
                                goto basic_json_parser_30;
                            }
                            goto basic_json_parser_32;
                        }
                        else
                        {
                            if (yych <= 's')
                            {
                                if (yych <= 'r')
                                {
                                    goto basic_json_parser_30;
                                }
                                goto basic_json_parser_32;
                            }
                            else
                            {
                                if (yych <= 't')
                                {
                                    goto basic_json_parser_30;
                                }
                                if (yych <= 'u')
                                {
                                    goto basic_json_parser_55;
                                }
                                goto basic_json_parser_32;
                            }
                        }
                    }
basic_json_parser_36:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 0x7F)
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= 0xBF)
                    {
                        goto basic_json_parser_30;
                    }
                    goto basic_json_parser_32;
basic_json_parser_37:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 0x9F)
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= 0xBF)
                    {
                        goto basic_json_parser_36;
                    }
                    goto basic_json_parser_32;
basic_json_parser_38:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 0x7F)
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= 0xBF)
                    {
                        goto basic_json_parser_36;
                    }
                    goto basic_json_parser_32;
basic_json_parser_39:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 0x7F)
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= 0x9F)
                    {
                        goto basic_json_parser_36;
                    }
                    goto basic_json_parser_32;
basic_json_parser_40:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 0x8F)
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= 0xBF)
                    {
                        goto basic_json_parser_38;
                    }
                    goto basic_json_parser_32;
basic_json_parser_41:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 0x7F)
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= 0xBF)
                    {
                        goto basic_json_parser_38;
                    }
                    goto basic_json_parser_32;
basic_json_parser_42:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 0x7F)
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= 0x8F)
                    {
                        goto basic_json_parser_38;
                    }
                    goto basic_json_parser_32;
basic_json_parser_43:
                    yyaccept = 2;
                    yych = *(m_marker = ++m_cursor);
                    if (yych <= '9')
                    {
                        if (yych == '.')
                        {
                            goto basic_json_parser_47;
                        }
                        if (yych >= '0')
                        {
                            goto basic_json_parser_48;
                        }
                    }
                    else
                    {
                        if (yych <= 'E')
                        {
                            if (yych >= 'E')
                            {
                                goto basic_json_parser_51;
                            }
                        }
                        else
                        {
                            if (yych == 'e')
                            {
                                goto basic_json_parser_51;
                            }
                        }
                    }
basic_json_parser_44:
                    {
                        last_token_type = token_type::value_integer;
                        break;
                    }
basic_json_parser_45:
                    yyaccept = 2;
                    m_marker = ++m_cursor;
                    if ((m_limit - m_cursor) < 3)
                    {
                        fill_line_buffer(3);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= '9')
                    {
                        if (yych == '.')
                        {
                            goto basic_json_parser_47;
                        }
                        if (yych <= '/')
                        {
                            goto basic_json_parser_44;
                        }
                        goto basic_json_parser_45;
                    }
                    else
                    {
                        if (yych <= 'E')
                        {
                            if (yych <= 'D')
                            {
                                goto basic_json_parser_44;
                            }
                            goto basic_json_parser_51;
                        }
                        else
                        {
                            if (yych == 'e')
                            {
                                goto basic_json_parser_51;
                            }
                            goto basic_json_parser_44;
                        }
                    }
basic_json_parser_47:
                    yych = *++m_cursor;
                    if (yych <= '/')
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych <= '9')
                    {
                        goto basic_json_parser_56;
                    }
                    goto basic_json_parser_32;
basic_json_parser_48:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= '/')
                    {
                        goto basic_json_parser_50;
                    }
                    if (yych <= '9')
                    {
                        goto basic_json_parser_48;
                    }
basic_json_parser_50:
                    {
                        last_token_type = token_type::parse_error;
                        break;
                    }
basic_json_parser_51:
                    yych = *++m_cursor;
                    if (yych <= ',')
                    {
                        if (yych == '+')
                        {
                            goto basic_json_parser_59;
                        }
                        goto basic_json_parser_32;
                    }
                    else
                    {
                        if (yych <= '-')
                        {
                            goto basic_json_parser_59;
                        }
                        if (yych <= '/')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= '9')
                        {
                            goto basic_json_parser_60;
                        }
                        goto basic_json_parser_32;
                    }
basic_json_parser_52:
                    yych = *++m_cursor;
                    if (yych == 'l')
                    {
                        goto basic_json_parser_62;
                    }
                    goto basic_json_parser_32;
basic_json_parser_53:
                    yych = *++m_cursor;
                    if (yych == 'l')
                    {
                        goto basic_json_parser_63;
                    }
                    goto basic_json_parser_32;
basic_json_parser_54:
                    yych = *++m_cursor;
                    if (yych == 'u')
                    {
                        goto basic_json_parser_64;
                    }
                    goto basic_json_parser_32;
basic_json_parser_55:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= '@')
                    {
                        if (yych <= '/')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= '9')
                        {
                            goto basic_json_parser_65;
                        }
                        goto basic_json_parser_32;
                    }
                    else
                    {
                        if (yych <= 'F')
                        {
                            goto basic_json_parser_65;
                        }
                        if (yych <= '`')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= 'f')
                        {
                            goto basic_json_parser_65;
                        }
                        goto basic_json_parser_32;
                    }
basic_json_parser_56:
                    yyaccept = 3;
                    m_marker = ++m_cursor;
                    if ((m_limit - m_cursor) < 3)
                    {
                        fill_line_buffer(3);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= 'D')
                    {
                        if (yych <= '/')
                        {
                            goto basic_json_parser_58;
                        }
                        if (yych <= '9')
                        {
                            goto basic_json_parser_56;
                        }
                    }
                    else
                    {
                        if (yych <= 'E')
                        {
                            goto basic_json_parser_51;
                        }
                        if (yych == 'e')
                        {
                            goto basic_json_parser_51;
                        }
                    }
basic_json_parser_58:
                    {
                        last_token_type = token_type::value_float;
                        break;
                    }
basic_json_parser_59:
                    yych = *++m_cursor;
                    if (yych <= '/')
                    {
                        goto basic_json_parser_32;
                    }
                    if (yych >= ':')
                    {
                        goto basic_json_parser_32;
                    }
basic_json_parser_60:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= '/')
                    {
                        goto basic_json_parser_58;
                    }
                    if (yych <= '9')
                    {
                        goto basic_json_parser_60;
                    }
                    goto basic_json_parser_58;
basic_json_parser_62:
                    yych = *++m_cursor;
                    if (yych == 's')
                    {
                        goto basic_json_parser_66;
                    }
                    goto basic_json_parser_32;
basic_json_parser_63:
                    yych = *++m_cursor;
                    if (yych == 'l')
                    {
                        goto basic_json_parser_67;
                    }
                    goto basic_json_parser_32;
basic_json_parser_64:
                    yych = *++m_cursor;
                    if (yych == 'e')
                    {
                        goto basic_json_parser_69;
                    }
                    goto basic_json_parser_32;
basic_json_parser_65:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= '@')
                    {
                        if (yych <= '/')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= '9')
                        {
                            goto basic_json_parser_71;
                        }
                        goto basic_json_parser_32;
                    }
                    else
                    {
                        if (yych <= 'F')
                        {
                            goto basic_json_parser_71;
                        }
                        if (yych <= '`')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= 'f')
                        {
                            goto basic_json_parser_71;
                        }
                        goto basic_json_parser_32;
                    }
basic_json_parser_66:
                    yych = *++m_cursor;
                    if (yych == 'e')
                    {
                        goto basic_json_parser_72;
                    }
                    goto basic_json_parser_32;
basic_json_parser_67:
                    ++m_cursor;
                    {
                        last_token_type = token_type::literal_null;
                        break;
                    }
basic_json_parser_69:
                    ++m_cursor;
                    {
                        last_token_type = token_type::literal_true;
                        break;
                    }
basic_json_parser_71:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= '@')
                    {
                        if (yych <= '/')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= '9')
                        {
                            goto basic_json_parser_74;
                        }
                        goto basic_json_parser_32;
                    }
                    else
                    {
                        if (yych <= 'F')
                        {
                            goto basic_json_parser_74;
                        }
                        if (yych <= '`')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= 'f')
                        {
                            goto basic_json_parser_74;
                        }
                        goto basic_json_parser_32;
                    }
basic_json_parser_72:
                    ++m_cursor;
                    {
                        last_token_type = token_type::literal_false;
                        break;
                    }
basic_json_parser_74:
                    ++m_cursor;
                    if (m_limit <= m_cursor)
                    {
                        fill_line_buffer(1);    // LCOV_EXCL_LINE
                    }
                    yych = *m_cursor;
                    if (yych <= '@')
                    {
                        if (yych <= '/')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= '9')
                        {
                            goto basic_json_parser_30;
                        }
                        goto basic_json_parser_32;
                    }
                    else
                    {
                        if (yych <= 'F')
                        {
                            goto basic_json_parser_30;
                        }
                        if (yych <= '`')
                        {
                            goto basic_json_parser_32;
                        }
                        if (yych <= 'f')
                        {
                            goto basic_json_parser_30;
                        }
                        goto basic_json_parser_32;
                    }
                }

            }

            position += static_cast<size_t>((m_cursor - m_start));
            return last_token_type;
        }

        /*!
        @brief append data from the stream to the line buffer

        This function is called by the scan() function when the end of the
        buffer (`m_limit`) is reached and the `m_cursor` pointer cannot be
        incremented without leaving the limits of the line buffer. Note re2c
        decides when to call this function.

        If the lexer reads from contiguous storage, there is no trailing null
        byte. Therefore, this function must make sure to add these padding
        null bytes.

        If the lexer reads from an input stream, this function reads the next
        line of the input.

        @pre
            p p p p p p u u u u u x . . . . . .
            ^           ^       ^   ^
            m_content   m_start |   m_limit
                                m_cursor

        @post
            u u u u u x x x x x x x . . . . . .
            ^       ^               ^
            |       m_cursor        m_limit
            m_start
            m_content
        */
        void fill_line_buffer(size_t n = 0)
        {
            // if line buffer is used, m_content points to its data
            assert(m_line_buffer.empty()
                   or m_content == reinterpret_cast<const lexer_char_t*>(m_line_buffer.data()));

            // if line buffer is used, m_limit is set past the end of its data
            assert(m_line_buffer.empty()
                   or m_limit == m_content + m_line_buffer.size());

            // pointer relationships
            assert(m_content <= m_start);
            assert(m_start <= m_cursor);
            assert(m_cursor <= m_limit);
            assert(m_marker == nullptr or m_marker  <= m_limit);

            // number of processed characters (p)
            const auto num_processed_chars = static_cast<size_t>(m_start - m_content);
            // offset for m_marker wrt. to m_start
            const auto offset_marker = (m_marker == nullptr) ? 0 : m_marker - m_start;
            // number of unprocessed characters (u)
            const auto offset_cursor = m_cursor - m_start;

            // no stream is used or end of file is reached
            if (m_stream == nullptr or m_stream->eof())
            {
                // m_start may or may not be pointing into m_line_buffer at
                // this point. We trust the standard library to do the right
                // thing. See http://stackoverflow.com/q/28142011/266378
                m_line_buffer.assign(m_start, m_limit);

                // append n characters to make sure that there is sufficient
                // space between m_cursor and m_limit
                m_line_buffer.append(1, '\x00');
                if (n > 0)
                {
                    m_line_buffer.append(n - 1, '\x01');
                }
            }
            else
            {
                // delete processed characters from line buffer
                m_line_buffer.erase(0, num_processed_chars);
                // read next line from input stream
                m_line_buffer_tmp.clear();

                // check if stream is still good
                if (m_stream->fail())
                {
                    JSON_THROW(parse_error(111, 0, "bad input stream"));
                }

                std::getline(*m_stream, m_line_buffer_tmp, '\n');

                // add line with newline symbol to the line buffer
                m_line_buffer += m_line_buffer_tmp;
                m_line_buffer.push_back('\n');
            }

            // set pointers
            m_content = reinterpret_cast<const lexer_char_t*>(m_line_buffer.data());
            assert(m_content != nullptr);
            m_start  = m_content;
            m_marker = m_start + offset_marker;
            m_cursor = m_start + offset_cursor;
            m_limit  = m_start + m_line_buffer.size();
        }

        /// return string representation of last read token
        string_t get_token_string() const
        {
            assert(m_start != nullptr);
            return string_t(reinterpret_cast<string_t::const_pointer>(m_start),
                            static_cast<size_t>(m_cursor - m_start));
        }

        /*!
        @brief return string value for string tokens

        The function iterates the characters between the opening and closing
        quotes of the string value. The complete string is the range
        [m_start,m_cursor). Consequently, we iterate from m_start+1 to
        m_cursor-1.

        We differentiate two cases:

        1. Escaped characters. In this case, a new character is constructed
           according to the nature of the escape. Some escapes create new
           characters (e.g., `"\\n"` is replaced by `"\n"`), some are copied
           as is (e.g., `"\\\\"`). Furthermore, Unicode escapes of the shape
           `"\\uxxxx"` need special care. In this case, to_unicode takes care
           of the construction of the values.
        2. Unescaped characters are copied as is.

        @pre `m_cursor - m_start >= 2`, meaning the length of the last token
        is at least 2 bytes which is trivially true for any string (which
        consists of at least two quotes).

            " c1 c2 c3 ... "
            ^                ^
            m_start          m_cursor

        @complexity Linear in the length of the string.\n

        Lemma: The loop body will always terminate.\n

        Proof (by contradiction): Assume the loop body does not terminate. As
        the loop body does not contain another loop, one of the called
        functions must never return. The called functions are `std::strtoul`
        and to_unicode. Neither function can loop forever, so the loop body
        will never loop forever which contradicts the assumption that the loop
        body does not terminate, q.e.d.\n

        Lemma: The loop condition for the for loop is eventually false.\n

        Proof (by contradiction): Assume the loop does not terminate. Due to
        the above lemma, this can only be due to a tautological loop
        condition; that is, the loop condition i < m_cursor - 1 must always be
        true. Let x be the change of i for any loop iteration. Then
        m_start + 1 + x < m_cursor - 1 must hold to loop indefinitely. This
        can be rephrased to m_cursor - m_start - 2 > x. With the
        precondition, we x <= 0, meaning that the loop condition holds
        indefinitely if i is always decreased. However, observe that the value
        of i is strictly increasing with each iteration, as it is incremented
        by 1 in the iteration expression and never decremented inside the loop
        body. Hence, the loop condition will eventually be false which
        contradicts the assumption that the loop condition is a tautology,
        q.e.d.

        @return string value of current token without opening and closing
        quotes
        @throw parse_error.102 if to_unicode fails or surrogate error
        @throw parse_error.103 if to_unicode fails
        */
        string_t get_string() const
        {
            assert(m_cursor - m_start >= 2);

            string_t result;
            result.reserve(static_cast<size_t>(m_cursor - m_start - 2));

            // iterate the result between the quotes
            for (const lexer_char_t* i = m_start + 1; i < m_cursor - 1; ++i)
            {
                // find next escape character
                auto e = std::find(i, m_cursor - 1, '\\');
                if (e != i)
                {
                    // see https://github.com/nlohmann/json/issues/365#issuecomment-262874705
                    for (auto k = i; k < e; k++)
                    {
                        result.push_back(static_cast<string_t::value_type>(*k));
                    }
                    i = e - 1; // -1 because of ++i
                }
                else
                {
                    // processing escaped character
                    // read next character
                    ++i;

                    switch (*i)
                    {
                        // the default escapes
                        case 't':
                        {
                            result += "\t";
                            break;
                        }
                        case 'b':
                        {
                            result += "\b";
                            break;
                        }
                        case 'f':
                        {
                            result += "\f";
                            break;
                        }
                        case 'n':
                        {
                            result += "\n";
                            break;
                        }
                        case 'r':
                        {
                            result += "\r";
                            break;
                        }
                        case '\\':
                        {
                            result += "\\";
                            break;
                        }
                        case '/':
                        {
                            result += "/";
                            break;
                        }
                        case '"':
                        {
                            result += "\"";
                            break;
                        }

                        // unicode
                        case 'u':
                        {
                            // get code xxxx from uxxxx
                            auto codepoint = std::strtoul(std::string(reinterpret_cast<string_t::const_pointer>(i + 1),
                                                          4).c_str(), nullptr, 16);

                            // check if codepoint is a high surrogate
                            if (codepoint >= 0xD800 and codepoint <= 0xDBFF)
                            {
                                // make sure there is a subsequent unicode
                                if ((i + 6 >= m_limit) or * (i + 5) != '\\' or * (i + 6) != 'u')
                                {
                                    JSON_THROW(parse_error(102, get_position(), "missing low surrogate"));
                                }

                                // get code yyyy from uxxxx\uyyyy
                                auto codepoint2 = std::strtoul(std::string(reinterpret_cast<string_t::const_pointer>
                                                               (i + 7), 4).c_str(), nullptr, 16);
                                result += to_unicode(codepoint, codepoint2);
                                // skip the next 10 characters (xxxx\uyyyy)
                                i += 10;
                            }
                            else if (codepoint >= 0xDC00 and codepoint <= 0xDFFF)
                            {
                                // we found a lone low surrogate
                                JSON_THROW(parse_error(102, get_position(), "missing high surrogate"));
                            }
                            else
                            {
                                // add unicode character(s)
                                result += to_unicode(codepoint);
                                // skip the next four characters (xxxx)
                                i += 4;
                            }
                            break;
                        }
                    }
                }
            }

            return result;
        }


        const size_t get_position() const
        {
            return position;
        }

      private:
        /// optional input stream
        std::istream* m_stream;
        /// line buffer buffer for m_stream
        string_t m_line_buffer;
        /// used for filling m_line_buffer
        string_t m_line_buffer_tmp;
        /// the buffer pointer
        const lexer_char_t* m_content;
        /// pointer to the beginning of the current symbol
        const lexer_char_t* m_start;
        /// pointer for backtracking information
        const lexer_char_t* m_marker;
        /// pointer to the current symbol
        const lexer_char_t* m_cursor;
        /// pointer to the end of the buffer
        const lexer_char_t* m_limit;
        /// the last token type
        token_type last_token_type;
        /// current position in the input (read bytes)
        size_t position;
    };

    /*!
    @brief syntax analysis

    This class implements a recursive decent parser.
    */
    class parser
    {
        parser();
      public:
        /// a parser reading from a string literal
        parser(const char* buff, const parser_callback_t cb = nullptr)
            : callback(cb),
              m_lexer(reinterpret_cast<const lexer::lexer_char_t*>(buff), std::strlen(buff))
            , depth(0)
            , last_token(lexer::token_type::uninitialized)
        {}

        /*!
        @brief a parser reading from an input stream
        @throw parse_error.111 if input stream is in a bad state
        */
        parser(std::istream& is, const parser_callback_t cb = nullptr)
            : callback(cb), m_lexer(is), depth(0)
            , last_token(lexer::token_type::uninitialized)
        {}

        /// a parser reading from an iterator range with contiguous storage
        template<class IteratorType>
        parser(IteratorType first, IteratorType last, const parser_callback_t cb = nullptr)
            : callback(cb),
              m_lexer(reinterpret_cast<const typename lexer::lexer_char_t*>(&(*first)),
                      static_cast<size_t>(std::distance(first, last)))
        {}

        /*!
        @brief public parser interface
        @throw parse_error.101 in case of an unexpected token
        @throw parse_error.102 if to_unicode fails or surrogate error
        @throw parse_error.103 if to_unicode fails
        */
        basic_json parse()
        {
            // read first token
            get_token();

            basic_json result = parse_internal(true);
            result.assert_invariant();

            expect(lexer::token_type::end_of_input);

            // return parser result and replace it with null in case the
            // top-level value was discarded by the callback function
            return result.is_discarded() ? basic_json() : std::move(basic_json(result));
        }

      private:
        /*!
        @brief the actual parser
        @throw parse_error.101 in case of an unexpected token
        @throw parse_error.102 if to_unicode fails or surrogate error
        @throw parse_error.103 if to_unicode fails
        */
        basic_json parse_internal(bool keep)
        {
            auto result = basic_json(value_t::discarded);

            switch (last_token)
            {
                case lexer::token_type::begin_object:
                {
                    if (keep and (not callback
                                  or ((keep = callback(depth++, parse_event_t::object_start, result)) != 0)))
                    {
                        // explicitly set result to object to cope with {}
                        result.m_type = value_t::object;
                        result.m_value = value_t::object;
                    }

                    // read next token
                    get_token();

                    // closing } -> we are done
                    if (last_token == lexer::token_type::end_object)
                    {
                        get_token();
                        if (keep and callback and not callback(--depth, parse_event_t::object_end, result))
                        {
                            result = basic_json(value_t::discarded);
                        }
                        return result;
                    }

                    // no comma is expected here
                    unexpect(lexer::token_type::value_separator);

                    // otherwise: parse key-value pairs
                    do
                    {
                        // ugly, but could be fixed with loop reorganization
                        if (last_token == lexer::token_type::value_separator)
                        {
                            get_token();
                        }

                        // store key
                        expect(lexer::token_type::value_string);
                        const auto key = m_lexer.get_string();

                        bool keep_tag = false;
                        if (keep)
                        {
                            if (callback)
                            {
                                basic_json k(key);
                                keep_tag = callback(depth, parse_event_t::key, k);
                            }
                            else
                            {
                                keep_tag = true;
                            }
                        }

                        // parse separator (:)
                        get_token();
                        expect(lexer::token_type::name_separator);

                        // parse and add value
                        get_token();
                        auto value = parse_internal(keep);
                        if (keep and keep_tag and not value.is_discarded())
                        {
                            result[key] = std::move(value);
                        }
                    }
                    while (last_token == lexer::token_type::value_separator);

                    // closing }
                    expect(lexer::token_type::end_object);
                    get_token();
                    if (keep and callback and not callback(--depth, parse_event_t::object_end, result))
                    {
                        result = basic_json(value_t::discarded);
                    }

                    return result;
                }

                case lexer::token_type::begin_array:
                {
                    // read next token
                    get_token();

                    // closing ] -> we are done
                    if (last_token == lexer::token_type::end_array)
                    {
                        get_token();
                        if (callback and not callback(--depth, parse_event_t::array_end, result))
                        {
                            result = basic_json(value_t::discarded);
                        }
                        return result;
                    }

                    // no comma is expected here
                    unexpect(lexer::token_type::value_separator);

                    // otherwise: parse values
                    do
                    {
                        // ugly, but could be fixed with loop reorganization
                        if (last_token == lexer::token_type::value_separator)
                        {
                            get_token();
                        }

                        // parse value
                        auto value = parse_internal(keep);
                    }
                    while (last_token == lexer::token_type::value_separator);

                    // closing ]
                    expect(lexer::token_type::end_array);
                    get_token();
                    if (keep and callback and not callback(--depth, parse_event_t::array_end, result))
                    {
                        result = basic_json(value_t::discarded);
                    }

                    return result;
                }

                case lexer::token_type::literal_null:
                {
                    get_token();
                    result.m_type = value_t::null;
                    break;
                }

                case lexer::token_type::value_string:
                {
                    const auto s = m_lexer.get_string();
                    get_token();
                    result = basic_json(s);
                    break;
                }

                case lexer::token_type::literal_true:
                case lexer::token_type::literal_false:
                case lexer::token_type::value_unsigned:
                case lexer::token_type::value_integer:
                case lexer::token_type::value_float:
                {
                    get_token();
                    result = basic_json(value_t::discarded);
                    break;
                }

                default:
                {
                    // the last token was unexpected
                    unexpect(last_token);
                }
            }

            if (keep and callback and not callback(depth, parse_event_t::value, result))
            {
                result = basic_json(value_t::discarded);
            }
            return result;
        }

        /// get next token from lexer
        lexer::token_type get_token()
        {
            last_token = m_lexer.scan();
            return last_token;
        }

        /*!
        @throw parse_error.101 if expected token did not occur
        */
        void expect(lexer::token_type t) const
        {
            if (t != last_token)
            {
                std::string error_msg = "parse error - unexpected ";
                error_msg += (last_token == lexer::token_type::parse_error ? ("'" +  m_lexer.get_token_string() +
                              "'") :
                              lexer::token_type_name(last_token));
                error_msg += "; expected " + lexer::token_type_name(t);
                JSON_THROW(parse_error(101, m_lexer.get_position(), error_msg));
            }
        }

        /*!
        @throw parse_error.101 if unexpected token occurred
        */
        void unexpect(lexer::token_type t) const
        {
            if (t == last_token)
            {
                std::string error_msg = "parse error - unexpected ";
                error_msg += (last_token == lexer::token_type::parse_error ? ("'" +  m_lexer.get_token_string() +
                              "'") :
                              lexer::token_type_name(last_token));
                JSON_THROW(parse_error(101, m_lexer.get_position(), error_msg));
            }
        }

      private:
        /// current level of recursion
        int depth;
        /// callback function
        const parser_callback_t callback;
        /// the type of the last read token
        lexer::token_type last_token;
        /// the lexer
        lexer m_lexer;
    };
};

/////////////
// presets //
/////////////

/*!
@brief default JSON class

This type is the default specialization of the @ref basic_json class which
uses the standard template types.

@since version 1.0.0
*/
typedef basic_json json;
} // namespace nlohmann


///////////////////////
// nonmember support //
///////////////////////

// specialization of std::swap, and std::hash
namespace std
{
/*!
@brief exchanges the values of two JSON objects

@since version 1.0.0
*/
template<>
inline void swap(nlohmann::json& j1,
                 nlohmann::json& j2)
{
    j1.swap(j2);
}

/// hash value for JSON objects
template<>
struct hash<nlohmann::json>
{
    /*!
    @brief return a hash value for a JSON object

    @since version 1.0.0
    */
    std::size_t operator()(const nlohmann::json& j) const
    {
        // a naive hashing via the string representation
        const auto& h = hash<nlohmann::json::string_t>();
        return h(j.dump());
    }
};

/// specialization for std::less<value_t>
template <>
struct less<::nlohmann::detail::value_t>
{
    /*!
    @brief compare two value_t enum values
    @since version 3.0.0
    */
    bool operator()(nlohmann::detail::value_t lhs,
                    nlohmann::detail::value_t rhs) const noexcept
    {
        return nlohmann::detail::operator<(lhs, rhs);
    }
};

} // namespace std

// restore GCC/clang diagnostic settings
#if defined(__clang__) || defined(__GNUC__) || defined(__GNUG__)
    #pragma GCC diagnostic pop
#endif

// clean up
#undef JSON_CATCH
#undef JSON_THROW
#undef JSON_TRY

#endif
