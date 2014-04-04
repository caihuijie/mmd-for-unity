﻿using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using System.Text;

// Reference URL:
//	  http://blog.goo.ne.jp/torisu_tetosuki/e/209ad341d3ece2b1b4df24abf619d6e4
//	  http://mikudan.blog120.fc2.com/blog-entry-280.html

namespace MMD
{
    namespace VMD
    {
        // VMDのフォーマットクラス
        public class VMDFormat
        {
            public string name;
            public string path;
            public string folder;

            public Header header;
            public MotionList motion_list;
            public SkinList skin_list;
            public LightList light_list;
            public CameraList camera_list;
            public SelfShadowList self_shadow_list;

            private class ToByteUtil
            {
                public static byte[] ListToBytes<M>(Dictionary<string, List<M>> list, uint list_count, int size)
                    where M : IBinary
                {
                    byte[] count = BitConverter.GetBytes(list_count);
                    byte[] retarr = new byte[list_count * size + 4];
                    SafeCopy(count, retarr, 0, 4);

                    int cnt = 0;
                    foreach (var mlist in list)
                    {
                        foreach (var m in mlist.Value)
                        {
                            byte[] bin = m.ToBytes();
                            SafeCopy(bin, retarr, cnt * size + 4, size);
                            cnt++;
                        }
                    }
                    return retarr;
                }

                public static byte[] ArrayToBytes<M>(M[] array, uint array_count, int size)
                    where M : IBinary
                {
                    byte[] count = BitConverter.GetBytes(array_count);
                    byte[] retarr = new byte[array_count * size + 4];
                    SafeCopy(count, retarr, 0, 4);

                    int cnt = 0;
                    foreach (var a in array)
                    {
                        byte[] bin = a.ToBytes();
                        SafeCopy(bin, retarr, cnt * size + 4, size);
                        cnt++;
                    }
                    return retarr;
                }

                public static byte[] Vector3ToBytes(ref Vector3 v)
                {
                    byte[] x = BitConverter.GetBytes(v.x);
                    byte[] y = BitConverter.GetBytes(v.y);
                    byte[] z = BitConverter.GetBytes(v.z);
                    List<byte> ret = new List<byte>();
                    ret.AddRange(x);
                    ret.AddRange(y);
                    ret.AddRange(z);
                    return ret.ToArray();
                }

                public static byte[] FloatRGBToBytes(ref Color c)
                {
                    byte[] r = BitConverter.GetBytes(c.r);
                    byte[] g = BitConverter.GetBytes(c.g);
                    byte[] b = BitConverter.GetBytes(c.b);
                    List<byte> ret = new List<byte>();
                    ret.AddRange(r);
                    ret.AddRange(g);
                    ret.AddRange(b);
                    return ret.ToArray();
                }

                public static void SafeCopy(byte[] source, byte[] dest, int start, int length)
                {
                    // Countがlengthになるまで数値を丸める
                    List<byte> retlist = new List<byte>(source);
                    if (retlist.Count < length)
                        while (retlist.Count != length) retlist.Add(0);
                    else if (retlist.Count > length)
                        while (retlist.Count != length) retlist.RemoveAt(retlist.Count - 1);

                    for (int i = 0; i < length; i++)
                    {
                        dest[start + i] = retlist[i];
                    }
                }

                public static byte[] EncodeUTFToSJIS(string str)
                {
                    Encoding src = Encoding.UTF8;
                    Encoding dest = Encoding.GetEncoding(932);
                    return Encoding.Convert(src, dest, src.GetBytes(str));
                }
            }

            public class Header : IBinary
            {
                public string vmd_header; // 30byte, "Vocaloid Motion Data 0002"
                public string vmd_model_name; // 20byte

                public byte[] ToBytes()
                {
                    byte[] header = Encoding.ASCII.GetBytes(vmd_header);
                    byte[] model_name = ToByteUtil.EncodeUTFToSJIS(vmd_model_name);
                    byte[] retarr = new byte[50];
                    ToByteUtil.SafeCopy(header, retarr, 0, 30);
                    ToByteUtil.SafeCopy(model_name, retarr, 30, 20);
                    return retarr;
                }
            }

            public class MotionList : IBinary
            {
                public uint motion_count;
                public Dictionary<string, List<Motion>> motion;

                public byte[] ToBytes()
                {
                    return ToByteUtil.ListToBytes(motion, motion_count, 111);
                }

                public void Insert(Motion motion)
                {
                    this.motion[motion.bone_name].Add(motion);
                    motion_count++;
                }
            }

            public class Motion : IBinary
            {
                public string bone_name;	// 15byte
                public uint frame_no;
                public Vector3 location;
                public Quaternion rotation;
                public byte[] interpolation;	// [4][4][4], 64byte

                // なんか不便になりそうな気がして
                public byte GetInterpolation(int i, int j, int k)
                {
                    return this.interpolation[i * 16 + j * 4 + k];
                }

                public void SetInterpolation(byte val, int i, int j, int k)
                {
                    this.interpolation[i * 16 + j * 4 + k] = val;
                }

                public byte[] ToBytes()
                {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            /// 表情リスト
            /// </summary>
            public class SkinList : IBinary
            {
                public uint skin_count;
                public Dictionary<string, List<SkinData>> skin = new Dictionary<string,List<SkinData>>();

                public byte[] ToBytes()
                {
                    return ToByteUtil.ListToBytes(skin, skin_count, 23);
                }

                public void Insert(SkinData skin_obj)
                {
                    if (!skin.ContainsKey(skin_obj.skin_name))
                        skin[skin_obj.skin_name] = new List<SkinData>();

                    skin[skin_obj.skin_name].Add(skin_obj);
                    skin_count++;
                }
            }

            public class SkinData : IBinary
            {
                public string skin_name;	// 15byte
                public uint frame_no;
                public float weight;

                public byte[] ToBytes()
                {
                    byte[] skin_name = ToByteUtil.EncodeUTFToSJIS(this.skin_name);
                    byte[] frame = BitConverter.GetBytes(frame_no);
                    byte[] weight = BitConverter.GetBytes(this.weight);
                    byte[] retarr = new byte[23];

                    ToByteUtil.SafeCopy(skin_name, retarr, 0, 15);
                    ToByteUtil.SafeCopy(frame, retarr, 15, 4);
                    ToByteUtil.SafeCopy(weight, retarr, 19, 4);
                    return retarr;
                }
            }

            public class CameraList : IBinary
            {
                public uint camera_count;
                public CameraData[] camera;

                public byte[] ToBytes()
                {
                    return ToByteUtil.ArrayToBytes(camera, camera_count, 61);
                }
            }

            public class CameraData : IBinary
            {
                public uint frame_no;
                public float length;
                public Vector3 location;
                public Vector3 rotation;	// オイラー角, X軸は符号が反転している
                public byte[] interpolation;	// [6][4], 24byte(未検証)
                public uint viewing_angle;
                public byte perspective;	// 0:on 1:off

                public byte GetInterpolation(int i, int j)
                {
                    return this.interpolation[i * 6 + j];
                }

                public void SetInterpolation(byte val, int i, int j)
                {
                    this.interpolation[i * 6 + j] = val;
                }

                public byte[] ToBytes()
                {
                    throw new NotImplementedException();
                }
            }

            public class LightList : IBinary
            {
                public uint light_count;
                public LightData[] light;

                public byte[] ToBytes()
                {
                    return ToByteUtil.ArrayToBytes(light, light_count, 28);
                }
            }

            public class LightData : IBinary
            {
                public uint frame_no;
                public Color rgb;	// αなし, 256
                public Vector3 location;

                public byte[] ToBytes()
                {
                    byte[] frame = BitConverter.GetBytes(frame_no);
                    byte[] rgb = ToByteUtil.FloatRGBToBytes(ref this.rgb);
                    byte[] location = ToByteUtil.Vector3ToBytes(ref this.location);
                    List<byte> retarr = new List<byte>();
                    retarr.AddRange(frame);
                    retarr.AddRange(rgb);
                    retarr.AddRange(location);
                    if (retarr.Count != 28) throw new IndexOutOfRangeException("照明データが28バイトちょうどじゃない");
                    return retarr.ToArray();
                }
            }

            public class SelfShadowList : IBinary
            {
                public uint self_shadow_count;
                public SelfShadowData[] self_shadow;

                public byte[] ToBytes()
                {
                    return ToByteUtil.ArrayToBytes(self_shadow, self_shadow_count, 9);
                }
            }

            public class SelfShadowData : IBinary
            {
                public uint frame_no;
                public byte mode; //00-02
                public float distance;	// 0.1 - (dist * 0.00001)

                public byte[] ToBytes()
                {
                    byte[] frame = BitConverter.GetBytes(frame_no);
                    byte[] mode = { this.mode };
                    byte[] distance = BitConverter.GetBytes(this.distance);
                    List<byte> retarr = new List<byte>();
                    retarr.AddRange(frame);
                    retarr.AddRange(mode);
                    retarr.AddRange(distance);
                    if (retarr.Count != 9) throw new IndexOutOfRangeException("セルフシャドウデータが9バイトちょうどじゃない");
                    return retarr.ToArray();
                }
            }
        }
    }
}