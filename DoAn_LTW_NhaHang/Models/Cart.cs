using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn_LTW_NhaHang.Models
{
    public class Cart
    {
        public List<CartItem> list;

        public Cart()
        {
            list = new List<CartItem>();
        }

        public Cart(List<CartItem> ds)
        {
            list = ds;
        }


        public int SoLuongMatHang()
        {
            return list.Count;
        }

        public int TongSL()
        {
            return list.Sum(x => x.SoLuong);
        }


        public decimal TongThanhTien()
        {
            return list.Sum(x => x.ThanhTien);
        }

        public int Tang(int id)
        {
            try
            {
                CartItem item = list.Find(x => x.MaSP == id);
                if (item == null)
                {
                    item = new CartItem(id);
                    list.Add(item);
                }
                else
                {
                    item.SoLuong++;
                }
                return 1;
            }
            catch (Exception e)
            {
                return -1;
            }
        }


        public int Giam(int id)
        {
            try
            {
                CartItem item = list.Find(x => x.MaSP == id);
                if (item != null)
                {
                    item.SoLuong--;
                    if (item.SoLuong <= 0)
                        list.Remove(item);
                }
                return 1;
            }
            catch (Exception e)
            {
                return -1;
            }
        }



        public int Xoa(int id)
        {
            try
            {
                CartItem item = list.Find(x => x.MaSP == id);
                if (item != null)
                {
                    list.Remove(item);
                }
                return 1;
            }
            catch (Exception e)
            {
                return -1;
            }
        }



    }
}