using FluentValidation;
using OrderManagement.Application.Orders.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.Commands.PlaceOrder
{
    public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
    {
        public PlaceOrderCommandValidator()
        {
            // Validate CustomerId
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId không được để trống");

            // Validate danh sách Items
            RuleFor(x => x.Items)
                .NotNull().WithMessage("Danh sách sản phẩm không được null")
                .NotEmpty().WithMessage("Đơn hàng phải có ít nhất 1 sản phẩm")
                .Must(items => items.Count <= 50)
                    .WithMessage("Đơn hàng không được vượt quá 50 sản phẩm");

            // Validate từng item trong danh sách
            RuleForEach(x => x.Items).SetValidator(new OrderItemDtoValidator());

            // Validate địa chỉ giao hàng
            RuleFor(x => x.ShippingAddress)
                .NotNull().WithMessage("Địa chỉ giao hàng không được bỏ trống")
                .SetValidator(new AddressDtoValidator());
        }
    }

    // Validator cho từng item
    public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
    {
        public OrderItemDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("ProductId không được để trống");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0")
                .LessThanOrEqualTo(1000).WithMessage("Số lượng không được vượt quá 1000");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage("Đơn giá phải lớn hơn 0")
                .LessThanOrEqualTo(100_000_000).WithMessage("Đơn giá vượt quá giới hạn cho phép");
        }
    }

    // Validator cho Address
    public class AddressDtoValidator : AbstractValidator<AddressDto>
    {
        public AddressDtoValidator()
        {
            RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Country).NotEmpty().Length(2, 3)
                .WithMessage("Country phải là ISO country code (2–3 ký tự)");
            RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        }
    }

}
