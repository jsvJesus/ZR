package scaleform.clik.interfaces
{
   public interface IScrollBar extends IUIComponent
   {
      function get position() : Number;
      
      function set position(param1:Number) : void;
      
      function setScrollProperties(param1:Number, param2:Number, param3:Number, param4:Number = NaN) : void;
   }
}

