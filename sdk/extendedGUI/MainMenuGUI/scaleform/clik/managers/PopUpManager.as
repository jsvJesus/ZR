package scaleform.clik.managers
{
   import flash.display.*;
   import flash.events.*;
   import flash.geom.*;
   import scaleform.gfx.*;
   
   public class PopUpManager
   {
      protected static var _stage:Stage;
      
      protected static var _defaultPopupCanvas:MovieClip;
      
      protected static var _modalMc:Sprite;
      
      protected static var _modalBg:Sprite;
      
      protected static var initialized:Boolean = false;
      
      public function PopUpManager()
      {
         super();
      }
      
      public static function init(stage:Stage) : void
      {
         if(initialized)
         {
            return;
         }
         PopUpManager._stage = stage;
         _defaultPopupCanvas = new MovieClip();
         _stage.addChild(_defaultPopupCanvas);
         stage.addEventListener(Event.ADDED,PopUpManager.handleStageAddedEvent,false,0,true);
         initialized = true;
      }
      
      public static function show(mc:DisplayObject, x:Number = 0, y:Number = 0, scope:DisplayObjectContainer = null) : void
      {
         if(!_stage)
         {
            return;
         }
         if(mc.parent)
         {
            mc.parent.removeChild(mc);
         }
         handleStageAddedEvent(null);
         _defaultPopupCanvas.addChild(mc);
         if(!scope)
         {
            scope = _stage;
         }
         var p:Point = new Point(x,y);
         p = scope.localToGlobal(p);
         mc.x = p.x;
         mc.y = p.y;
      }
      
      public static function showModal(mc:Sprite, bg:Sprite = null, controllerIdx:uint = 0) : void
      {
         if(!_stage)
         {
            return;
         }
         if(_modalMc)
         {
            _defaultPopupCanvas.removeChild(_modalMc);
         }
         if(!mc)
         {
            return;
         }
         if(mc.parent)
         {
            mc.parent.removeChild(mc);
         }
         if(Boolean(bg) && Boolean(bg.parent))
         {
            bg.parent.removeChild(mc);
         }
         _modalMc = mc;
         _modalBg = bg;
         if(_modalBg)
         {
            _defaultPopupCanvas.addChild(_modalBg);
         }
         _defaultPopupCanvas.addChild(_modalMc);
         _modalMc.addEventListener(Event.REMOVED_FROM_STAGE,handleRemoveModalMc,false,0,true);
         FocusManager.setModalClip(_modalMc,controllerIdx);
      }
      
      protected static function handleStageAddedEvent(e:Event) : void
      {
         _stage.setChildIndex(_defaultPopupCanvas,_stage.numChildren - 1);
      }
      
      protected static function handleRemoveModalMc(e:Event) : void
      {
         _modalBg.removeEventListener(Event.REMOVED_FROM_STAGE,handleRemoveModalMc,false);
         if(_modalBg)
         {
            _defaultPopupCanvas.removeChild(_modalBg);
         }
         _modalMc = null;
         _modalBg = null;
      }
   }
}

