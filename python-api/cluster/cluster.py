import time
import requests
import json
from enum import Enum


class Actions(Enum):
    """Enumeration of recognized actions.

    The actions that are recognized by the connector and therefore can be returned are enumerated in this class.
    To loop through all of the actions in this enumeration, simply use

        for action in Actions:
               # do something with action
    """

    MATCH_QUESTIONS = "match_questions"
    """Match questions."""

    ESTIMATE_OFFENSIVENESS = "estimate_offensiveness"
    """Estimate the offensiveness of a question."""

    @classmethod
    def has_value(cls, value):
        return value in cls._value2member_map_


class Connector(object):
    """Allows communication with Cluster API server.

    This Connector class allows communication with the Cluster API server by returning NLP tasks
    from the server whenever any are available and by replying with a response.

    Attributes:
        prefetch: A boolean that enables this Connector to fetch all available tasks. If `prefetch` is set to False,
            only one task will be fetched at a time. To improve performance you may want to leave this set to True,
            because that way less data transfers may be needed, though there's no guaranty for that.
    """

    def __init__(self):
        """
        Raises:
            Exception: Something went wrong while sending the reply to the server.
                This exception may become more specific in a future release, but for now it is kept as general as
                possible, so any implementation changes don't effect these specifications.
        """
        self.prefetch = True
        self._tasks = list()  # store non processed received tasks
        self._tasks_in_progress = dict()  # keep track of work in progress
        self._server_timeout = 4  # timeout used while checking for server messages
        self._base_request_uri = "https://clusterapi20200320113808.azurewebsites.net/api/nlp"

    def has_task(self) -> bool:
        """Checks whether the server has any tasks available.

        Asks the server to check whether it has any tasks that should be processed.
        This method should only be used when there is no reason to use `get_next_task()` afterwards. Because that kind
        of situations seems to be quite uncommon, this method will likely be removed in one of the next releases.

        Returns:
            True if and only if there is a task to be processed.
        """
        return True

    def get_next_task(self, timeout=None) -> any:
        """
        Waits for the next task from the server and returns it as a dictionary.

        Waits until the server has delivered a task or until timeout if a timeout is set.

        Args:
            timeout: The number of seconds to wait before returning without result. In case the timeout is set to None,
                then the method will only return upon receiving a task from the server.

        Currently two possible JSON structures can be expected:

        1. The server asks to match a question with an undefined number of questions:

                {
                    "action": Answers.MATCH_QUESTIONS,
                    "question_id": 123,
                    "question": "XXX",
                    "compare_questions": [
                        {
                            "question_id": 111,
                            "question": "AAA"
                        },
                        {
                            "question_id": 222,
                            "question": "BBB"
                        },
                        {
                            "question_id": 333,
                            "question": "CCC"
                        },
                    ],
                    "msg_id": 1234567890
                }


        2. The server asks to estimate the offensiveness of a sentence:

                 {
                    "action": Answers.ESTIMATE_OFFENSIVENESS,
                    "question_id": 100,
                    "question": "XXX",
                    "msg_id": 1234567890
                 }

        Returns:
             A task to be processed as a JSON object or None when no task was received before timeout.

        Raises:
            Exception: something went wrong while communicating with the server.
                This exception may become more specific in a future release, but for now it is kept as general as
                possible, so any implementation changes don't effect these specifications.
        """
        """
        TODO(Joren) 1st iteration:
            Send a simple HTTP request to API server requesting task to be performed.
            Append received tasks to _tasks and return first item of list if not empty (shouldn`t be 
            possible, because this method only ends when a task has been received and appended to _tasks).
        
        TODO(Joren) 1st-2nd iteration: 
            Return first element of _tasks and update _tasks in background without causing delay in case _tasks is not
            empty.
        
        TODO(Joren) 2nd-3rd iteration:
            Connect to server using web socket, so a permanent connection is made. This way the server
            can push directly any tasks without this client having to poll every now and then.
        """

        tasks_found = False
        if len(self._tasks) == 0:
            # no tasks left, ask the server
            if timeout is None or timeout > 0:
                if timeout is not None:
                    # equally divide the given timeout
                    timeout_offensive = timeout / 2
                    timeout_unmatched = timeout - timeout_offensive
                else:
                    timeout_unmatched = None
                    timeout_offensive = None
                path_unmatched = "/questions/unmatched"
                tasks_found = self._request_questions(path_unmatched, timeout_unmatched)

                if self.prefetch or not tasks_found:
                    # request questions of which the offensiveness has to be tested
                    path_offensive = "/questions/offensive/undefined"
                    # if prefetching disabled and already task found, then don't look for another task
                    tasks_found = tasks_found | self._request_questions(path_offensive, timeout_offensive)

            if not tasks_found:
                return None
        task = self._tasks.pop(0)
        self._tasks_in_progress[task['msg_id']] = task

        return task

    def _request_questions(self, path: str, timeout: int):
        """Sends a request to the server to receive tasks."""
        request_uri = self._base_request_uri + path
        request = requests.get(request_uri, timeout=timeout)
        if request.status_code == 200:
            # Status == OK
            # JSON response can be in different format than the one that should be returned
            received_tasks = self._parse_response(request.json())
            if self.prefetch:
                # fetch all available tasks
                new_task_found = False
                for task in received_tasks:
                    if task not in self._tasks and task['msg_id'] not in self._tasks_in_progress:
                        # only add task if not in the (processing) task list already
                        self._tasks.append(task)
                        new_task_found = True
                return new_task_found
            else:
                # prefetching disabled, so only fetch one question that is not in the task list already
                for task in received_tasks:
                    if task not in self._tasks and task['msg_id'] not in self._tasks_in_progress:
                        self._tasks.append(task)
                        return True
                # all available tasks have been fetched before
                return False
        return False

    @classmethod
    def _parse_response(cls, response: dict) -> dict:
        """Processes a dictionary received from the server and returns a dictionary that complies to
        structure of the result of `get_next_task()`.

        Args:
            response: The response from the server as a dictionary.

        Returns:
            A dictionary that complies to the structure of the result of `get_next_task()` containing the
            information of the given `response` as far as the structure allows it.
        """
        return response

    def reply(self, response: dict) -> dict:
        """Sends the given response to the server.

        Args:
            response: A dictionary built like a JSON object.

            The effect of replying with a response that doesn't follow one of the below mentioned structures
            is undefined. As a response argument, currently two possible structures are allowed:

            1. A reply to a `match_question` containing a top x of comparable questions:

                    {
                        "question_id": 123,
                        "possible_matches": [
                            {
                                "question_id": 111,
                                "prob": 0.789
                            },
                            {
                                "question_id": 333,
                                "prob": 0.654
                            }
                        ],
                        "msg_id": 1234567890
                    }

            2. A reply to an `estimate_offensiveness`:

                    {
                        "question_id": 100,
                        "prob": 0.123,
                        "msg_id": 1234567890
                    }

            The `msg_id` is always used to include in the reply so that the server knows to
            which task the reply belongs. It corresponds to the `msg_id` from a task from
            the `get_next_task()` method.

        Raises:
            Exception: something went wrong while sending the reply to the server.
                This exception may become more specific in a future release, but for now it is kept as general as
                possible, so any implementation changes don't effect these specifications.
        """

        question_id = response['question_id']
        print("TASKS IN PROGRESS:", self._tasks_in_progress)
        print("TASKS:", self._tasks)
        action = self._tasks_in_progress[response['msg_id']]['action']
        if Actions.has_value(action) and response['msg_id'] in self._tasks_in_progress.keys():
            request_uri = self._base_request_uri
            if action == Actions.MATCH_QUESTIONS.value:
                request_uri += f"/questions/match/{question_id}"
            elif action == Actions.ESTIMATE_OFFENSIVENESS.value:
                request_uri += f"/questions/offensive/{question_id}"
            del self._tasks_in_progress[response['msg_id']]
            data = response
            request = requests.post(request_uri, json=data)
            return request.json()
