package com.dvalimona.components
{
   public class Style
   {
      public static var MENU_LABEL_COLOR:uint = 12895428;
      
      public static var MENU_LABEL_OVER_COLOR:uint = 10347511;
      
      public static var MENU_LABEL_DOWN_COLOR:uint = 2855599;
      
      public static var TEXT_BACKGROUND:uint = 16777215;
      
      public static var BACKGROUND:uint = 13421772;
      
      public static var BUTTON_UP:uint = 0;
      
      public static var BUTTON_OVER:uint = 0;
      
      public static var BUTTON_DOWN:uint = 460551;
      
      public static var BUTTON_UP_ALPHA:Number = 0.8;
      
      public static var BUTTON_OVER_ALPHA:Number = 0.8;
      
      public static var BUTTON_DOWN_ALPHA:Number = 1;
      
      public static var BUTTON_UP_LABEL_COLOR:Number = 16777215;
      
      public static var BUTTON_OVER_LABEL_COLOR:Number = 5626367;
      
      public static var BUTTON_DOWN_LABEL_COLOR:Number = 2855599;
      
      public static var BUTTON_UP_LABEL_ALPHA:Number = 0.8;
      
      public static var BUTTON_OVER_LABEL_ALPHA:Number = 0.8;
      
      public static var BUTTON_DOWN_LABEL_ALPHA:Number = 0.8;
      
      public static var PANEL:uint = 0;
      
      public static var BLACK_PANEL_COLOR:uint = 0;
      
      public static var BLACK_PANEL_ALPHA:Number = 0.8;
      
      public static var INPUT_COLOR_IN:uint = 16777215;
      
      public static var INPUT_COLOR_OUT:uint = 16777215;
      
      public static var INPUT_COLOR_ALPHA_IN:Number = 1;
      
      public static var INPUT_COLOR_ALPHA_OUT:Number = 0.8;
      
      public static var INPUT_LABEL_COLOR_IN:uint = 0;
      
      public static var INPUT_LABEL_COLOR_OUT:uint = 0;
      
      public static var INPUT_LABEL_COLOR_ALPHA_IN:Number = 1;
      
      public static var INPUT_LABEL_COLOR_ALPHA_OUT:Number = 0.8;
      
      public static var CHECK_WIDTH:uint = 50;
      
      public static var CHECK_HEIGHT:uint = 26;
      
      public static var CHECK_ROUNDNESS:Number = 1;
      
      public static var CHECK_GAP:uint = 0;
      
      public static var CHECK_BORDER_WIDTH:uint = 3;
      
      public static var CHECK_SPACING:Number = 1;
      
      public static var CHECK_ON_COLOR:uint = 2081541;
      
      public static var CHECK_ON_ALPHA:Number = 0.33;
      
      public static var CHECK_OFF_COLOR:uint = 0;
      
      public static var CHECK_OFF_ALPHA:Number = 0.33;
      
      public static var CHECK_BODY_COLOR:uint = 16777215;
      
      public static var CHECK_BODY_ALPHA:Number = 1;
      
      public static var CHECK_LABEL_COLOR:uint = 16777215;
      
      public static var SLIDER_HANDLE_WIDTH:uint = 50;
      
      public static var SLIDER_HANDLE_HEIGHT:uint = 25;
      
      public static var SLIDER_THICKNESS:uint = 3;
      
      public static var SLIDER_BACK_CORNER:uint = 5;
      
      public static var SLIDER_BACK_COLOR:uint = 16777215;
      
      public static var SLIDER_HANDLE_COLOR:uint = 16777215;
      
      public static var SLIDER_LABEL_COLOR:uint = 2236962;
      
      public static var SLIDER_LABEL_SIZE:uint = 20;
      
      public static var SLIDER_ANIMATION_TIME:Number = 0.3;
      
      public static var INPUT_TEXT:uint = 3355443;
      
      public static var LABEL_TEXT:uint = 16777215;
      
      public static var DROPSHADOW:uint = 0;
      
      public static var PROGRESS_BAR:uint = 16777215;
      
      public static var LIST_DEFAULT:uint = 16777215;
      
      public static var LIST_ALTERNATE:uint = 15987699;
      
      public static var LIST_SELECTED:uint = 13421772;
      
      public static var LIST_ROLLOVER:uint = 14540253;
      
      public static var DEBUG_COLOR:uint = 16711680;
      
      public static var DEBUG_ALPHA:Number = 0.25;
      
      public static var embedFonts:Boolean = true;
      
      public static var fontBlackName:String = "StalkerBlack";
      
      public static var fontLightName:String = "StalkerLight";
      
      public static var fontSize:Number = 18;
      
      public static var LABEL_FONT_SIZE:Number = 38;
      
      public static const DARK:String = "dark";
      
      public static const LIGHT:String = "light";
      
      public function Style()
      {
         super();
      }
      
      public static function get lightFontName() : String
      {
         return Base.FONT_LIGHT;
      }
      
      public static function get fontName() : String
      {
         return Base.FONT_REGULAR;
      }
      
      public static function get boldFontName() : String
      {
         return Base.FONT_BOLD;
      }
      
      public static function setStyle(style:String) : void
      {
         switch(style)
         {
            case DARK:
               Style.BACKGROUND = 4473924;
               Style.BUTTON_UP = 6710886;
               Style.BUTTON_DOWN = 2236962;
               Style.INPUT_TEXT = 12303291;
               Style.LABEL_TEXT = 13421772;
               Style.PANEL = 6710886;
               Style.PROGRESS_BAR = 6710886;
               Style.TEXT_BACKGROUND = 5592405;
               Style.LIST_DEFAULT = 4473924;
               Style.LIST_ALTERNATE = 3750201;
               Style.LIST_SELECTED = 6710886;
               Style.LIST_ROLLOVER = 7829367;
               break;
            case LIGHT:
            default:
               Style.BACKGROUND = 13421772;
               Style.BUTTON_UP = 16777215;
               Style.BUTTON_DOWN = 15658734;
               Style.INPUT_TEXT = 3355443;
               Style.LABEL_TEXT = 6710886;
               Style.PANEL = 15987699;
               Style.PROGRESS_BAR = 16777215;
               Style.TEXT_BACKGROUND = 16777215;
               Style.LIST_DEFAULT = 16777215;
               Style.LIST_ALTERNATE = 15987699;
               Style.LIST_SELECTED = 13421772;
               Style.LIST_ROLLOVER = 14540253;
         }
      }
   }
}

