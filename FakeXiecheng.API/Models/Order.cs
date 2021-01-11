using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FakeXiecheng.API.Models
{
    public enum OrderStateEnum
    {
        Pending, //订单已经生产
        Processing, //支付处理中
        Completed, //交易成功
        Declined, //交易失败
        Cancelled, //订单取消
        Refund, //已退款
    }
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public ICollection<LineItem> OrderItems { get; set; }
        public OrderStateEnum State { get; set; }
        public DateTime CreateDateUTC { get; set; }
        public string TransactionMetadata { get; set; }
    }
}
 