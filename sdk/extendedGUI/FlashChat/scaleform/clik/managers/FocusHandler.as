package scaleform.clik.managers
{
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.InteractiveObject;
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.events.Event;
   import flash.events.FocusEvent;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.utils.Dictionary;
   import scaleform.clik.constants.InputValue;
   import scaleform.clik.constants.NavigationCode;
   import scaleform.clik.core.CLIK;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.events.FocusHandlerEvent;
   import scaleform.clik.events.InputEvent;
   import scaleform.clik.ui.InputDetails;
   import scaleform.gfx.Extensions;
   import scaleform.gfx.FocusEventEx;
   import scaleform.gfx.FocusManager;
   
   public class FocusHandler
   {
      public static var instance:FocusHandler;
      
      protected static var initialized:Boolean = false;
      
      protected var _stage:Stage;
      
      protected var currentFocusLookup:Dictionary;
      
      protected var actualFocusLookup:Dictionary;
      
      protected var preventStageFocusChanges:Boolean = false;
      
      protected var mouseDown:Boolean = false;
      
      public function FocusHandler()
      {
         super();
         this.currentFocusLookup = new Dictionary(true);
         this.actualFocusLookup = new Dictionary(true);
      }
      
      public static function getInstance() : FocusHandler
      {
         if(instance == null)
         {
            instance = new FocusHandler();
         }
         return instance;
      }
      
      public static function init(stage:Stage, component:UIComponent) : void
      {
         if(initialized)
         {
            return;
         }
         var focusHandler:FocusHandler = FocusHandler.getInstance();
         focusHandler.stage = stage;
         FocusManager.alwaysEnableArrowKeys = true;
         FocusManager.disableFocusKeys = true;
         initialized = true;
      }
      
      public function set stage(value:Stage) : void
      {
         if(this._stage == null)
         {
            this._stage = value;
         }
         this._stage.stageFocusRect = false;
         if(Extensions.enabled)
         {
            this._stage.addEventListener(MouseEvent.MOUSE_DOWN,this.trackMouseDown,false,0,true);
            this._stage.addEventListener(MouseEvent.MOUSE_UP,this.trackMouseDown,false,0,true);
         }
         this._stage.addEventListener(FocusEvent.FOCUS_IN,this.updateActualFocus,false,0,true);
         this._stage.addEventListener(FocusEvent.FOCUS_OUT,this.updateActualFocus,false,0,true);
         this._stage.addEventListener(FocusEvent.KEY_FOCUS_CHANGE,this.handleMouseFocusChange,false,0,true);
         this._stage.addEventListener(FocusEvent.MOUSE_FOCUS_CHANGE,this.handleMouseFocusChange,false,0,true);
         var inputDelegate:InputDelegate = InputDelegate.getInstance();
         inputDelegate.initialize(this._stage);
         inputDelegate.addEventListener(InputEvent.INPUT,this.handleInput,false,0,true);
      }
      
      public function getFocus(index:uint) : DisplayObject
      {
         return this.currentFocusLookup[index] as DisplayObject;
      }
      
      public function setFocus(focus:DisplayObject, index:uint = 0, mouseChange:Boolean = false) : void
      {
         var focusComponent:UIComponent = null;
         var focusParam:DisplayObject = focus;
         if(focus != null)
         {
            while(true)
            {
               focusComponent = focus as UIComponent;
               if(focusComponent == null)
               {
                  break;
               }
               if(focusComponent.focusTarget == null)
               {
                  break;
               }
               focus = focusComponent.focusTarget;
            }
         }
         if(focusComponent != null)
         {
            if(!focusComponent.focusable)
            {
               focus = null;
            }
         }
         var spr:Sprite = focus as Sprite;
         if(spr && mouseChange && spr.tabEnabled == false)
         {
            focus = null;
         }
         if(CLIK.disableNullFocusMoves && (focus == null || focus == this._stage))
         {
            return;
         }
         var actualFocus:DisplayObject = this.actualFocusLookup[index];
         var currentFocus:DisplayObject = this.currentFocusLookup[index];
         if(currentFocus != focus)
         {
            focusComponent = currentFocus as UIComponent;
            if(focusComponent != null)
            {
               focusComponent.focused &= ~(1 << index);
            }
            if(currentFocus != null)
            {
               currentFocus.dispatchEvent(new FocusHandlerEvent(FocusHandlerEvent.FOCUS_OUT,true,false,index));
            }
            currentFocus = focus;
            this.currentFocusLookup[index] = focus;
            focusComponent = currentFocus as UIComponent;
            if(focusComponent != null)
            {
               focusComponent.focused |= 1 << index;
            }
            if(currentFocus != null)
            {
               currentFocus.dispatchEvent(new FocusHandlerEvent(FocusHandlerEvent.FOCUS_IN,true,false,index));
            }
         }
         var isActualFocusTextField:* = actualFocus is TextField;
         var isCurrentFocusUIComponent:* = currentFocus is UIComponent;
         if(actualFocus != currentFocus && (!isActualFocusTextField || isActualFocusTextField && !isCurrentFocusUIComponent))
         {
            if(focusParam is TextField && focusParam != focus && focus == null)
            {
               this.preventStageFocusChanges = true;
               this._stage.focus = focusParam as InteractiveObject;
            }
            else
            {
               this.preventStageFocusChanges = true;
               this._stage.focus = focus as InteractiveObject;
            }
            this._stage.addEventListener(Event.ENTER_FRAME,this.clearFocusPrevention,false,0,true);
         }
      }
      
      protected function clearFocusPrevention(e:Event) : void
      {
         this.preventStageFocusChanges = false;
         this._stage.removeEventListener(Event.ENTER_FRAME,this.clearFocusPrevention,false);
      }
      
      public function input(details:InputDetails) : void
      {
         var event:* = new InputEvent(InputEvent.INPUT,details);
         this.handleInput(event);
      }
      
      public function trackMouseDown(e:MouseEvent) : void
      {
         this.mouseDown = e.buttonDown;
      }
      
      protected function handleInput(event:InputEvent) : void
      {
         var focusProp:String = null;
         var index:Number = event.details.controllerIndex;
         var component:InteractiveObject = this.currentFocusLookup[index];
         if(component == null)
         {
            component = this._stage;
         }
         var newEvent:InputEvent = event.clone() as InputEvent;
         var ok:Boolean = component.dispatchEvent(newEvent);
         if(!ok || newEvent.handled)
         {
            return;
         }
         if(event.details.value == InputValue.KEY_UP)
         {
            return;
         }
         var nav:String = event.details.navEquivalent;
         if(nav == null)
         {
            return;
         }
         var focusedElement:InteractiveObject = this.currentFocusLookup[index];
         var actualFocus:InteractiveObject = this.actualFocusLookup[index];
         var stageFocusedElement:InteractiveObject = this._stage.focus;
         if(actualFocus is TextField && actualFocus == focusedElement && this.handleTextFieldInput(nav,index))
         {
            return;
         }
         if(actualFocus is TextField && this.handleTextFieldInput(nav,index))
         {
            return;
         }
         var dirX:Boolean = nav == NavigationCode.LEFT || nav == NavigationCode.RIGHT;
         var dirY:Boolean = nav == NavigationCode.UP || Boolean(NavigationCode.DOWN);
         if(focusedElement == null)
         {
            if(Boolean(this._stage.focus) && this._stage.focus is UIComponent)
            {
               focusedElement = this._stage.focus as UIComponent;
            }
         }
         if(focusedElement == null)
         {
            if(Boolean(actualFocus) && actualFocus is UIComponent)
            {
               focusedElement = actualFocus as UIComponent;
            }
         }
         if(focusedElement == null)
         {
            return;
         }
         var focusContext:DisplayObjectContainer = focusedElement.parent;
         var focusMode:String = FocusMode.DEFAULT;
         if(dirX || dirY)
         {
            focusProp = dirX ? FocusMode.HORIZONTAL : FocusMode.VERTICAL;
            while(focusContext != null)
            {
               if(!(focusProp in focusContext))
               {
                  break;
               }
               focusMode = focusContext[focusProp];
               if(focusMode != null && focusMode != FocusMode.DEFAULT)
               {
                  break;
               }
               focusContext = focusContext.parent;
            }
         }
         else
         {
            focusContext = null;
         }
         if(actualFocus is TextField && actualFocus.parent == focusedElement)
         {
            focusedElement = this._stage.focus;
         }
         var newFocus:InteractiveObject = FocusManager.findFocus(nav,null,focusMode == FocusMode.LOOP,focusedElement,false,index);
         if(newFocus != null)
         {
            this.setFocus(newFocus);
         }
      }
      
      protected function handleMouseFocusChange(event:FocusEvent) : void
      {
         this.handleFocusChange(event.target as InteractiveObject,event.relatedObject as InteractiveObject,event);
      }
      
      protected function handleFocusChange(oldFocus:InteractiveObject, newFocus:InteractiveObject, event:FocusEvent) : void
      {
         var focusTF:TextField = null;
         if(this.mouseDown && newFocus is TextField)
         {
            event.preventDefault();
            return;
         }
         if(CLIK.disableDynamicTextFieldFocus && newFocus is TextField)
         {
            focusTF = newFocus as TextField;
            if(focusTF.type == "dynamic")
            {
               event.stopImmediatePropagation();
               event.stopPropagation();
               event.preventDefault();
               return;
            }
         }
         if(newFocus is UIComponent)
         {
            event.preventDefault();
         }
         if(oldFocus is TextField && newFocus == null)
         {
            event.preventDefault();
            return;
         }
         var sfEvent:* = event as FocusEventEx;
         var controllerIndex:* = sfEvent == null ? 0 : sfEvent.controllerIdx;
         var index:uint = 0;
         this.actualFocusLookup[index] = newFocus;
         this.setFocus(newFocus,index,event.type == FocusEvent.MOUSE_FOCUS_CHANGE);
      }
      
      protected function updateActualFocus(event:FocusEvent) : void
      {
         var oldFocus:InteractiveObject = null;
         var newFocus:InteractiveObject = null;
         if(event.type == FocusEvent.FOCUS_IN)
         {
            oldFocus = event.relatedObject as InteractiveObject;
            newFocus = event.target as InteractiveObject;
         }
         else
         {
            oldFocus = event.target as InteractiveObject;
            newFocus = event.relatedObject as InteractiveObject;
         }
         if(event.type == FocusEvent.FOCUS_OUT)
         {
            if(this.preventStageFocusChanges)
            {
               event.stopImmediatePropagation();
               event.stopPropagation();
            }
         }
         var sfEvent:* = event as FocusEventEx;
         var controllerIndex:* = sfEvent == null ? 0 : sfEvent.controllerIdx;
         var index:uint = 0;
         this.actualFocusLookup[index] = newFocus;
         var currentFocus:InteractiveObject = this.currentFocusLookup[index];
         if(newFocus != null && newFocus is TextField && newFocus.parent != null && currentFocus == newFocus.parent && currentFocus == oldFocus)
         {
            return;
         }
         var isActualFocusTextField:* = newFocus is TextField;
         var isCurrentFocusUIComponent:* = currentFocus is UIComponent;
         if(newFocus != currentFocus)
         {
            if(!(isActualFocusTextField && isCurrentFocusUIComponent) || newFocus == null)
            {
               if(!this.preventStageFocusChanges || isActualFocusTextField)
               {
                  this.setFocus(newFocus);
               }
            }
         }
      }
      
      protected function handleTextFieldInput(nav:String, controllerIdx:uint) : Boolean
      {
         var actualFocus:TextField = this.actualFocusLookup[controllerIdx] as TextField;
         if(actualFocus == null)
         {
            return false;
         }
         var position:int = actualFocus.caretIndex;
         var focusIdx:Number = 0;
         switch(nav)
         {
            case NavigationCode.UP:
               if(!actualFocus.multiline)
               {
                  return false;
               }
               break;
            case NavigationCode.LEFT:
               break;
            case NavigationCode.DOWN:
               if(!actualFocus.multiline)
               {
                  return false;
               }
            case NavigationCode.RIGHT:
               return position < actualFocus.length;
            default:
               return false;
         }
         return position > 0;
      }
   }
}

