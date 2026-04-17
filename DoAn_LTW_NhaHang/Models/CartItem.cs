using DoAn_LTW_NhaHang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn_LTW_NhaHang.Models
{
    public class CartItem
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string AnhDaiDien { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien
        {
            get { return DonGia * SoLuong; }
        }

        QL_NhaHangEntities da = new QL_NhaHangEntities();
        public CartItem(int id)
        {
            tblMonAn item = da.tblMonAns.FirstOrDefault(x => x.MaMon == id);
            if (item != null)
            {
                MaSP = item.MaMon;
                TenSP = item.TenMon;
                AnhDaiDien = item.AnhDaiDien;
                DonGia = item.DonGia.Value;
                SoLuong = 1;
            }
        }
    }
}